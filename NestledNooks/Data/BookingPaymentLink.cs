namespace NestledNooks.Data;

/// <summary>Guest payment link for Stripe Checkout (full, deposit, or balance).</summary>
public sealed class BookingPaymentLink
{
    public int Id { get; set; }

    public int BookingRequestId { get; set; }

    public BookingRequest BookingRequest { get; set; } = null!;

    /// <summary>URL-safe token for /pay/{token}.</summary>
    public string Token { get; set; } = "";

    public string Purpose { get; set; } = BookingPaymentPurposes.Full;

    public decimal Amount { get; set; }

    public string? StripeCheckoutSessionId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}
