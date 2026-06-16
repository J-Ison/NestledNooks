using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed class BookingRequestService : IBookingRequestService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly IBookingAvailabilityService _availability;
    private readonly BookingPricingService _pricing;
    private readonly IStripePaymentService _stripe;
    private readonly IGuestEmailWrapperService _guestEmailWrapper;
    private readonly ISiteSettingsService _siteSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly StripeOptions _stripeOptions;
    private readonly ILogger<BookingRequestService> _logger;

    public BookingRequestService(
        ApplicationDbContext db,
        IEmailService email,
        IBookingAvailabilityService availability,
        BookingPricingService pricing,
        IStripePaymentService stripe,
        IGuestEmailWrapperService guestEmailWrapper,
        ISiteSettingsService siteSettings,
        IHttpContextAccessor httpContextAccessor,
        IOptions<StripeOptions> stripeOptions,
        ILogger<BookingRequestService> logger)
    {
        _db = db;
        _email = email;
        _availability = availability;
        _pricing = pricing;
        _stripe = stripe;
        _guestEmailWrapper = guestEmailWrapper;
        _siteSettings = siteSettings;
        _httpContextAccessor = httpContextAccessor;
        _stripeOptions = stripeOptions.Value;
        _logger = logger;
    }

    public async Task<BookingSubmitResult> SubmitAsync(
        BookingFormModel model,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var siteSettings = await _siteSettings.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!siteSettings.DirectBookingsEnabled &&
            !HostStaffAuthorization.IsOwner(_httpContextAccessor.HttpContext?.User))
        {
            return Fail(
                BookingSubmitErrorCodes.DirectBookingDisabled,
                "Direct booking is temporarily unavailable. Please contact us or check back soon.");
        }

        var slug = model.PropertySlug.Trim().ToLowerInvariant();
        var property = _pricing.GetProperty(slug);
        if (property is null)
            return Fail(BookingSubmitErrorCodes.UnknownProperty, "Unknown property.");

        if (model.CheckIn is not { } checkIn || model.CheckOut is not { } checkOut)
            return Fail(BookingSubmitErrorCodes.MissingDates, "Check-in and check-out are required.");

        if (model.GuestCount > property.MaxGuests)
            return Fail(BookingSubmitErrorCodes.TooManyGuests, $"Maximum {property.MaxGuests} guests.");

        if (model.PetCount > property.MaxPets)
            return Fail(BookingSubmitErrorCodes.TooManyPets, $"Maximum {property.MaxPets} pets.");

        BookingQuote quote;
        try
        {
            quote = await _pricing.CalculateAsync(slug, checkIn, checkOut, model.PetCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            return Fail(BookingSubmitErrorCodes.QuoteFailed, ex.Message);
        }

        if (!await _availability.IsRangeAvailableAsync(slug, checkIn, checkOut, cancellationToken: cancellationToken)
                .ConfigureAwait(false))
        {
            return Fail(
                BookingSubmitErrorCodes.DatesUnavailable,
                "Those dates are no longer available. Please choose different dates.");
        }

        var entity = new BookingRequest
        {
            UserId = userId,
            PropertySlug = slug,
            GuestFullName = model.GuestFullName.Trim(),
            GuestEmail = model.GuestEmail.Trim(),
            GuestPhone = string.IsNullOrWhiteSpace(model.GuestPhone) ? null : model.GuestPhone.Trim(),
            CheckIn = checkIn,
            CheckOut = checkOut,
            GuestCount = model.GuestCount,
            PetCount = model.PetCount,
            NightCount = quote.Nights,
            NightlyRate = quote.NightlyRate,
            CleaningFee = quote.CleaningFee,
            PetFee = quote.PetFee,
            Subtotal = quote.Subtotal,
            TotalAmount = quote.TotalAmount,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            Status = BookingStatuses.Pending,
            PaymentStatus = PaymentStatuses.Unpaid,
            AmountPaid = 0,
            PaymentReceivedAtUtc = null,
            CreatedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow,
            // Unique index on BookingNumber; final value assigned after Id is generated.
            BookingNumber = Guid.NewGuid().ToString("N")
        };

        _db.BookingRequests.Add(entity);
        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            entity.BookingNumber = $"NN-{DateTime.UtcNow:yyyyMMdd}-{entity.Id:D5}";
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex)
        {
            var detail = ex.InnerException?.Message ?? ex.Message;
            _logger.LogError(ex, "Booking save failed ({ErrorCode}).", BookingSubmitErrorCodes.SaveFailed);
            return Fail(BookingSubmitErrorCodes.SaveFailed, $"Could not save booking: {detail}");
        }

        var emailPayload = new BookingRequestEmailPayload(
            entity.Id,
            entity.BookingNumber,
            property.DisplayName,
            entity.GuestFullName,
            entity.GuestEmail,
            entity.GuestPhone,
            entity.CheckIn,
            entity.CheckOut,
            entity.GuestCount,
            entity.PetCount,
            entity.NightCount,
            entity.TotalAmount,
            entity.Notes);

        string? emailWarning = null;
        try
        {
            await _email.SendBookingRequestEmail(emailPayload).ConfigureAwait(false);
            await _email.SendBookingRequestGuestConfirmationEmail(emailPayload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Booking {BookingNumber} saved but confirmation emails were not sent",
                entity.BookingNumber);
            emailWarning =
                "Your request was saved, but we could not send email (check SMTP settings). " +
                "We will still review your booking.";
        }

        return new BookingSubmitResult(true, entity.Id, entity.BookingNumber, null, null, emailWarning);
    }

    public async Task<BookingConfirmationSummary?> GetPublicConfirmationAsync(
        string bookingNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bookingNumber))
            return null;

        var normalized = bookingNumber.Trim();
        var entity = await _db.BookingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingNumber == normalized, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return null;

        var property = _pricing.GetProperty(entity.PropertySlug);
        var displayName = property?.DisplayName ?? entity.PropertySlug;
        var (checkInTime, checkOutTime) = PropertyStayTimes.Resolve(property);

        return new BookingConfirmationSummary(
            entity.BookingNumber,
            entity.PropertySlug,
            displayName,
            entity.GuestFullName,
            entity.CheckIn,
            entity.CheckOut,
            checkInTime,
            checkOutTime,
            entity.NightCount,
            entity.TotalAmount,
            entity.Status);
    }

    private static BookingSubmitResult Fail(string code, string message) =>
        new(false, null, null, code, message);

    public async Task<BookingQuote?> GetQuoteAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int petCount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _pricing.CalculateAsync(propertySlug, checkIn, checkOut, petCount, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<BookingRequest>> GetForUserAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<BookingRequest>> GetPendingForPropertyAsync(
        string? propertySlug = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.Status == BookingStatuses.Pending);

        if (!string.IsNullOrWhiteSpace(propertySlug))
            query = query.Where(b => b.PropertySlug == propertySlug);

        return await query
            .OrderBy(b => b.CheckIn)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BookingRequest>> GetAllForAdminAsync(
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BookingRequests.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(b => b.Status == status);

        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<BookingStatusUpdateResult> UpdatePaymentAsync(
        int bookingId,
        string paymentStatus,
        decimal? amountPaid,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedPaymentStatus(paymentStatus))
            return new BookingStatusUpdateResult(false, "Invalid payment status.");

        var entity = await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new BookingStatusUpdateResult(false, "Booking not found.");

        entity.PaymentStatus = paymentStatus;

        switch (paymentStatus)
        {
            case PaymentStatuses.Paid:
                entity.AmountPaid = amountPaid ?? entity.TotalAmount;
                entity.PaymentReceivedAtUtc = DateTime.UtcNow;
                break;
            case PaymentStatuses.PartiallyPaid:
                if (amountPaid is null or < 0)
                    return new BookingStatusUpdateResult(false, "Enter amount paid for partial payment.");
                entity.AmountPaid = amountPaid.Value;
                entity.PaymentReceivedAtUtc = DateTime.UtcNow;
                break;
            case PaymentStatuses.Unpaid:
                entity.AmountPaid = 0;
                entity.PaymentReceivedAtUtc = null;
                break;
            case PaymentStatuses.Refunded:
                entity.AmountPaid = amountPaid ?? entity.AmountPaid;
                entity.PaymentReceivedAtUtc ??= DateTime.UtcNow;
                break;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BookingStatusUpdateResult(true, null);
    }

    public async Task<BookingApprovalResult> ApproveForFullPaymentAsync(
        int bookingId,
        string? statusNote,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_stripe.IsConfigured)
            return new BookingApprovalResult(false, "Stripe is not configured. Add API keys under Payment settings.", null);

        var entity = await LoadBookingForUpdateAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new BookingApprovalResult(false, "Booking not found.", null);

        if (entity.Status != BookingStatuses.Pending)
            return new BookingApprovalResult(false, "Only pending requests can be approved for payment.", null);

        entity.RequiredDepositAmount = null;
        entity.DepositNonRefundable = false;

        var statusResult = await ApplyStatusChangeAsync(entity, BookingStatuses.Approved, statusNote, cancellationToken)
            .ConfigureAwait(false);
        if (!statusResult.Succeeded)
            return new BookingApprovalResult(false, statusResult.ErrorMessage, null);

        var linkResult = await _stripe.CreatePaymentLinkAsync(
            entity,
            BookingPaymentPurposes.Full,
            entity.TotalAmount,
            siteBaseUrl,
            cancellationToken).ConfigureAwait(false);

        if (!linkResult.Succeeded || linkResult.PayUrl is null)
            return new BookingApprovalResult(false, linkResult.ErrorMessage ?? "Could not create payment link.", null);

        var emailWarning = await SendPaymentEmailAsync(
            entity,
            linkResult.PayUrl,
            entity.TotalAmount,
            "Amount due now",
            nonRefundable: false).ConfigureAwait(false);

        return new BookingApprovalResult(true, null, linkResult.PayUrl, emailWarning);
    }

    public async Task<BookingApprovalResult> ApproveWithDepositAsync(
        int bookingId,
        decimal depositAmount,
        string? statusNote,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_stripe.IsConfigured)
            return new BookingApprovalResult(false, "Stripe is not configured. Add API keys under Payment settings.", null);

        var entity = await LoadBookingForUpdateAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new BookingApprovalResult(false, "Booking not found.", null);

        if (entity.Status != BookingStatuses.Pending)
            return new BookingApprovalResult(false, "Only pending requests can be approved with a deposit.", null);

        depositAmount = decimal.Round(depositAmount, 2, MidpointRounding.AwayFromZero);
        var minimumDeposit = CalculateMinimumDeposit(entity.TotalAmount);
        if (depositAmount < minimumDeposit)
        {
            return new BookingApprovalResult(
                false,
                $"Deposit must be at least {minimumDeposit:C2} ({_stripeOptions.DefaultMinimumDepositPercent}% of the booking total).",
                null);
        }

        if (depositAmount >= entity.TotalAmount)
        {
            return new BookingApprovalResult(
                false,
                "Deposit must be less than the full total. Use full payment approval instead.",
                null);
        }

        entity.RequiredDepositAmount = depositAmount;
        entity.DepositNonRefundable = true;

        var statusResult = await ApplyStatusChangeAsync(entity, BookingStatuses.Approved, statusNote, cancellationToken)
            .ConfigureAwait(false);
        if (!statusResult.Succeeded)
            return new BookingApprovalResult(false, statusResult.ErrorMessage, null);

        var linkResult = await _stripe.CreatePaymentLinkAsync(
            entity,
            BookingPaymentPurposes.Deposit,
            depositAmount,
            siteBaseUrl,
            cancellationToken).ConfigureAwait(false);

        if (!linkResult.Succeeded || linkResult.PayUrl is null)
            return new BookingApprovalResult(false, linkResult.ErrorMessage ?? "Could not create payment link.", null);

        var emailWarning = await SendPaymentEmailAsync(
            entity,
            linkResult.PayUrl,
            depositAmount,
            "Non-refundable deposit due now",
            nonRefundable: true).ConfigureAwait(false);

        return new BookingApprovalResult(true, null, linkResult.PayUrl, emailWarning);
    }

    public async Task<BookingPaymentLinkResult> CreateBalancePaymentLinkAsync(
        int bookingId,
        string siteBaseUrl,
        CancellationToken cancellationToken = default)
    {
        if (!_stripe.IsConfigured)
            return new BookingPaymentLinkResult(false, "Stripe is not configured.", null, null);

        var entity = await _db.BookingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new BookingPaymentLinkResult(false, "Booking not found.", null, null);

        if (entity.Status is not (BookingStatuses.Approved or BookingStatuses.Active))
            return new BookingPaymentLinkResult(false, "Booking is not approved.", null, null);

        var balance = decimal.Round(entity.TotalAmount - entity.AmountPaid, 2, MidpointRounding.AwayFromZero);
        if (balance <= 0)
            return new BookingPaymentLinkResult(false, "No balance remaining on this booking.", null, null);

        var linkResult = await _stripe.CreatePaymentLinkAsync(
            entity,
            BookingPaymentPurposes.Balance,
            balance,
            siteBaseUrl,
            cancellationToken).ConfigureAwait(false);

        if (!linkResult.Succeeded || linkResult.PayUrl is null)
            return linkResult;

        var emailWarning = await SendPaymentEmailAsync(
            entity,
            linkResult.PayUrl,
            balance,
            "Remaining balance due",
            nonRefundable: false).ConfigureAwait(false);

        return linkResult with { EmailWarning = emailWarning };
    }

    public async Task<string?> GetLatestPaymentUrlAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        var urls = await GetLatestPaymentUrlsAsync([bookingId], cancellationToken).ConfigureAwait(false);
        return urls.TryGetValue(bookingId, out var url) ? url : null;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetLatestPaymentUrlsAsync(
        IEnumerable<int> bookingIds,
        CancellationToken cancellationToken = default)
    {
        var ids = bookingIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        var links = await _db.BookingPaymentLinks
            .AsNoTracking()
            .Where(l => ids.Contains(l.BookingRequestId) && l.CompletedAtUtc == null)
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var result = new Dictionary<int, string>();
        foreach (var link in links)
        {
            if (!result.ContainsKey(link.BookingRequestId))
                result[link.BookingRequestId] = $"/pay/{link.Token}";
        }

        return result;
    }

    public async Task<BookingStatusUpdateResult> SendGuestEmailAsync(
        int bookingId,
        string message,
        string? emailSubject = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            return new BookingStatusUpdateResult(false, "Enter a message before sending.");

        var entity = await _db.BookingRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new BookingStatusUpdateResult(false, "Booking not found.");

        var property = _pricing.GetProperty(entity.PropertySlug);
        var displayName = property?.DisplayName ?? entity.PropertySlug;
        var payUrl = await GetLatestPaymentUrlAsync(bookingId, cancellationToken).ConfigureAwait(false);

        var payload = new BookingGuestMessageEmailPayload(
            entity.BookingNumber,
            displayName,
            entity.GuestFullName,
            entity.GuestEmail,
            entity.CheckIn,
            entity.CheckOut,
            entity.NightCount,
            entity.TotalAmount,
            message.Trim(),
            emailSubject,
            payUrl);

        try
        {
            var fullBody = await _guestEmailWrapper.ComposeFullBodyAsync(message.Trim(), payload, cancellationToken)
                .ConfigureAwait(false);

            await _email.SendBookingGuestMessageEmailAsync(payload with { Message = fullBody })
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Guest email failed for booking {BookingNumber}.", entity.BookingNumber);
            return new BookingStatusUpdateResult(
                false,
                "Could not send email. Check SMTP settings (Smtp:Password in user secrets).");
        }

        return new BookingStatusUpdateResult(true, null);
    }

    public async Task<BookingStatusUpdateResult> SyncPaymentFromStripeAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        if (!_stripe.IsConfigured)
            return new BookingStatusUpdateResult(false, "Stripe is not configured.");

        var sessionId = await _db.BookingPaymentLinks
            .AsNoTracking()
            .Where(l => l.BookingRequestId == bookingId && l.CompletedAtUtc == null)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => l.StripeCheckoutSessionId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new BookingStatusUpdateResult(
                false,
                "No open Stripe checkout session found for this booking.");
        }

        var result = await _stripe.ConfirmCheckoutSessionAsync(sessionId, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Succeeded)
            return new BookingStatusUpdateResult(false, result.ErrorMessage ?? "Could not sync payment from Stripe.");

        if (result.PaymentApplied)
            return new BookingStatusUpdateResult(true, null);

        return new BookingStatusUpdateResult(true, null);
    }

    public async Task<BookingStatusUpdateResult> UpdateStatusAsync(
        int bookingId,
        string newStatus,
        string? statusNote,
        CancellationToken cancellationToken = default)
    {
        if (!IsAllowedTransition(newStatus))
            return new BookingStatusUpdateResult(false, "Invalid status.");

        if (newStatus == BookingStatuses.Approved)
        {
            return new BookingStatusUpdateResult(
                false,
                "Use Approve & request payment or Approve with deposit for approved bookings.");
        }

        var entity = await LoadBookingForUpdateAsync(bookingId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new BookingStatusUpdateResult(false, "Booking not found.");

        return await ApplyStatusChangeAsync(entity, newStatus, statusNote, cancellationToken).ConfigureAwait(false);
    }

    private async Task<BookingRequest?> LoadBookingForUpdateAsync(
        int bookingId,
        CancellationToken cancellationToken) =>
        await _db.BookingRequests
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken)
            .ConfigureAwait(false);

    private async Task<BookingStatusUpdateResult> ApplyStatusChangeAsync(
        BookingRequest entity,
        string newStatus,
        string? statusNote,
        CancellationToken cancellationToken)
    {
        var oldStatus = entity.Status;

        if (newStatus is BookingStatuses.Approved or BookingStatuses.Active)
        {
            if (!await _availability.IsRangeAvailableAsync(
                    entity.PropertySlug,
                    entity.CheckIn,
                    entity.CheckOut,
                    entity.Id,
                    cancellationToken).ConfigureAwait(false))
            {
                return new BookingStatusUpdateResult(
                    false,
                    "Cannot approve — dates conflict with another booking or external calendar.");
            }
        }

        entity.Status = newStatus;
        entity.StatusNote = string.IsNullOrWhiteSpace(statusNote) ? null : statusNote.Trim();
        entity.StatusUpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var property = _pricing.GetProperty(entity.PropertySlug);
        var displayName = property?.DisplayName ?? entity.PropertySlug;

        try
        {
            await _email.SendBookingStatusChangedEmailsAsync(new BookingStatusEmailPayload(
                entity.BookingNumber,
                displayName,
                entity.GuestFullName,
                entity.GuestEmail,
                entity.CheckIn,
                entity.CheckOut,
                entity.TotalAmount,
                oldStatus,
                newStatus,
                entity.StatusNote)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status updated but notification email failed for {BookingNumber}.", entity.BookingNumber);
        }

        return new BookingStatusUpdateResult(true, null);
    }

    private async Task<string?> SendPaymentEmailAsync(
        BookingRequest entity,
        string paymentUrl,
        decimal amountDue,
        string paymentLabel,
        bool nonRefundable)
    {
        var property = _pricing.GetProperty(entity.PropertySlug);
        var displayName = property?.DisplayName ?? entity.PropertySlug;

        try
        {
            await _email.SendBookingPaymentRequestEmailAsync(new BookingPaymentEmailPayload(
                entity.BookingNumber,
                displayName,
                entity.GuestFullName,
                entity.GuestEmail,
                entity.CheckIn,
                entity.CheckOut,
                amountDue,
                entity.TotalAmount,
                paymentUrl,
                paymentLabel,
                nonRefundable)).ConfigureAwait(false);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Payment link created but email failed for {BookingNumber}.", entity.BookingNumber);
            return
                "Approved, but the payment email could not be sent (check SMTP settings). " +
                "Copy the pay link below and send it to the guest manually.";
        }
    }

    private decimal CalculateMinimumDeposit(decimal totalAmount)
    {
        var percent = Math.Clamp(_stripeOptions.DefaultMinimumDepositPercent, 1, 100);
        return decimal.Round(totalAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsAllowedTransition(string status) =>
        status is BookingStatuses.Approved
            or BookingStatuses.Denied
            or BookingStatuses.Cancelled
            or BookingStatuses.Active
            or BookingStatuses.Ended;

    private static bool IsAllowedPaymentStatus(string status) =>
        status is PaymentStatuses.Unpaid
            or PaymentStatuses.PartiallyPaid
            or PaymentStatuses.Paid
            or PaymentStatuses.Refunded;
}
