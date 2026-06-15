using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using Stripe;
using Stripe.Checkout;

namespace NestledNooks.Services;

public interface IStripePaymentService
{
    bool IsConfigured { get; }

    StripeOptions Options { get; }

    Task<BookingPaymentLinkResult> CreatePaymentLinkAsync(
        BookingRequest booking,
        string purpose,
        decimal amount,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);

    Task<string?> GetCheckoutRedirectUrlAsync(
        string token,
        string siteBaseUrl,
        CancellationToken cancellationToken = default);

    Task HandleWebhookAsync(
        string json,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies payment when the guest lands on the success page (fallback when webhooks are not configured).
    /// Idempotent — safe if the webhook already ran.
    /// </summary>
    Task<StripeCheckoutConfirmResult> ConfirmCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

public sealed record StripeCheckoutConfirmResult(
    bool Succeeded,
    bool PaymentApplied,
    string? BookingNumber,
    string? ErrorMessage);

public sealed record BookingPaymentLinkResult(
    bool Succeeded,
    string? ErrorMessage,
    BookingPaymentLink? Link,
    string? PayUrl,
    string? EmailWarning = null);

public sealed class StripePaymentService : IStripePaymentService
{
    private readonly ApplicationDbContext _db;
    private readonly StripeOptions _options;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        ApplicationDbContext db,
        IOptions<StripeOptions> options,
        ILogger<StripePaymentService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public StripeOptions Options => _options;

    public async Task<BookingPaymentLinkResult> CreatePaymentLinkAsync(
        BookingRequest booking,
        string purpose,
        decimal amount,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new BookingPaymentLinkResult(false, "Stripe is not configured.", null, null);

        if (amount <= 0)
            return new BookingPaymentLinkResult(false, "Payment amount must be greater than zero.", null, null);

        var token = Guid.NewGuid().ToString("N");
        var link = new BookingPaymentLink
        {
            BookingRequestId = booking.Id,
            Token = token,
            Purpose = purpose,
            Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero),
            CreatedAtUtc = DateTime.UtcNow,
        };

        _db.BookingPaymentLinks.Add(link);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var payUrl = BuildPayUrl(siteBaseUrl, token);
        return new BookingPaymentLinkResult(true, null, link, payUrl);
    }

    public async Task<string?> GetCheckoutRedirectUrlAsync(
        string token,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var link = await _db.BookingPaymentLinks
            .Include(l => l.BookingRequest)
            .FirstOrDefaultAsync(l => l.Token == token, cancellationToken)
            .ConfigureAwait(false);

        if (link is null || link.CompletedAtUtc is not null)
            return null;

        var booking = link.BookingRequest;
        if (booking.Status is not (BookingStatuses.Approved or BookingStatuses.Active))
            return null;

        StripeConfiguration.ApiKey = _options.SecretKey!.Trim();

        if (!string.IsNullOrWhiteSpace(link.StripeCheckoutSessionId))
        {
            var existing = await new SessionService().GetAsync(link.StripeCheckoutSessionId, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (existing.Status == "open" && !string.IsNullOrWhiteSpace(existing.Url))
                return existing.Url;
        }

        var session = await CreateCheckoutSessionAsync(link, booking, siteBaseUrl, cancellationToken)
            .ConfigureAwait(false);

        link.StripeCheckoutSessionId = session.Id;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return session.Url;
    }

    public async Task HandleWebhookAsync(
        string json,
        string stripeSignatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            _logger.LogWarning("Stripe webhook received but WebhookSecret is not configured.");
            return;
        }

        StripeConfiguration.ApiKey = _options.SecretKey!.Trim();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignatureHeader,
                _options.WebhookSecret.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            throw;
        }

        if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session
                ?? throw new InvalidOperationException("Checkout session payload missing.");

            await ApplyCheckoutCompletedAsync(session, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<StripeCheckoutConfirmResult> ConfirmCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new StripeCheckoutConfirmResult(false, false, null, "Stripe is not configured.");

        if (string.IsNullOrWhiteSpace(sessionId))
            return new StripeCheckoutConfirmResult(false, false, null, "Missing checkout session.");

        StripeConfiguration.ApiKey = _options.SecretKey!.Trim();

        Session session;
        try
        {
            session = await new SessionService()
                .GetAsync(sessionId.Trim(), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve Stripe checkout session {SessionId}.", sessionId);
            return new StripeCheckoutConfirmResult(false, false, null, "Could not verify payment with Stripe.");
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return new StripeCheckoutConfirmResult(
                false,
                false,
                null,
                "Payment is not complete yet. If you were charged, wait a moment and refresh.");
        }

        if (!session.Metadata.TryGetValue("booking_id", out var bookingIdRaw) ||
            !int.TryParse(bookingIdRaw, out var bookingId))
        {
            return new StripeCheckoutConfirmResult(false, false, null, "Could not match this payment to a booking.");
        }

        var booking = await _db.BookingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
            return new StripeCheckoutConfirmResult(false, false, null, "Booking not found.");

        if (!session.Metadata.TryGetValue("payment_link_id", out var linkIdRaw) ||
            !int.TryParse(linkIdRaw, out var linkId))
        {
            return new StripeCheckoutConfirmResult(false, false, booking.BookingNumber, "Could not match payment link.");
        }

        var link = await _db.BookingPaymentLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken)
            .ConfigureAwait(false);

        if (link?.CompletedAtUtc is not null)
        {
            return new StripeCheckoutConfirmResult(true, false, booking.BookingNumber, null);
        }

        await ApplyCheckoutCompletedAsync(session, cancellationToken).ConfigureAwait(false);
        return new StripeCheckoutConfirmResult(true, true, booking.BookingNumber, null);
    }

    private async Task ApplyCheckoutCompletedAsync(Session session, CancellationToken cancellationToken)
    {
        if (!session.Metadata.TryGetValue("booking_id", out var bookingIdRaw) ||
            !int.TryParse(bookingIdRaw, out var bookingId))
        {
            _logger.LogWarning("Checkout session {SessionId} missing booking_id metadata.", session.Id);
            return;
        }

        if (!session.Metadata.TryGetValue("payment_link_id", out var linkIdRaw) ||
            !int.TryParse(linkIdRaw, out var linkId))
        {
            _logger.LogWarning("Checkout session {SessionId} missing payment_link_id metadata.", session.Id);
            return;
        }

        var amountPaid = (session.AmountTotal ?? 0) / 100m;
        var booking = await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (booking is null)
            return;

        var link = await _db.BookingPaymentLinks
            .FirstOrDefaultAsync(l => l.Id == linkId, cancellationToken)
            .ConfigureAwait(false);

        if (link is null || link.CompletedAtUtc is not null)
            return;

        link.CompletedAtUtc = DateTime.UtcNow;
        link.StripeCheckoutSessionId = session.Id;

        booking.AmountPaid = decimal.Round(booking.AmountPaid + amountPaid, 2, MidpointRounding.AwayFromZero);
        booking.PaymentReceivedAtUtc = DateTime.UtcNow;

        if (booking.AmountPaid >= booking.TotalAmount)
            booking.PaymentStatus = PaymentStatuses.Paid;
        else
            booking.PaymentStatus = PaymentStatuses.PartiallyPaid;

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Stripe payment applied for booking {BookingNumber}: {Amount} ({Purpose}).",
            booking.BookingNumber,
            amountPaid,
            link.Purpose);
    }

    private async Task<Session> CreateCheckoutSessionAsync(
        BookingPaymentLink link,
        BookingRequest booking,
        string siteBaseUrl,
        CancellationToken cancellationToken)
    {
        var baseUrl = siteBaseUrl.TrimEnd('/');
        var purposeLabel = link.Purpose switch
        {
            BookingPaymentPurposes.Deposit => "Non-refundable deposit",
            BookingPaymentPurposes.Balance => "Remaining balance",
            _ => "Stay payment",
        };

        var description =
            $"{booking.CheckIn:MMM d, yyyy} → {booking.CheckOut:MMM d, yyyy} · {booking.NightCount} night(s)";

        if (link.Purpose == BookingPaymentPurposes.Deposit && booking.DepositNonRefundable)
            description += " · Non-refundable deposit";

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            CustomerEmail = booking.GuestEmail,
            ClientReferenceId = booking.BookingNumber,
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)Math.Round(link.Amount * 100m, MidpointRounding.AwayFromZero),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{purposeLabel} — {booking.BookingNumber}",
                            Description = description,
                        },
                    },
                },
            ],
            Metadata = new Dictionary<string, string>
            {
                ["booking_id"] = booking.Id.ToString(),
                ["payment_link_id"] = link.Id.ToString(),
                ["payment_purpose"] = link.Purpose,
            },
            SuccessUrl = $"{baseUrl}/booking/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/booking/payment/cancel?token={link.Token}",
        };

        return await new SessionService().CreateAsync(options, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildPayUrl(string siteBaseUrl, string token) =>
        $"{siteBaseUrl.TrimEnd('/')}/pay/{token}";
}
