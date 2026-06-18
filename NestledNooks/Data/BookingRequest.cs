namespace NestledNooks.Data;

public sealed class BookingRequest
{
    public int Id { get; set; }

    /// <summary>Public reference, e.g. NN-20260515-A3F2.</summary>
    public string BookingNumber { get; set; } = "";

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string PropertySlug { get; set; } = "";

    public string GuestFullName { get; set; } = "";
    public string GuestEmail { get; set; } = "";
    public string? GuestPhone { get; set; }

    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }

    public int GuestCount { get; set; }
    public int PetCount { get; set; }

    public int NightCount { get; set; }
    public decimal NightlyRate { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal PetFee { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public string Status { get; set; } = BookingStatuses.Pending;

    public string? StatusNote { get; set; }

    /// <summary>Unpaid until you record payment (e.g. after Stripe or manual deposit).</summary>
    public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;

    public decimal AmountPaid { get; set; }

    public DateTime? PaymentReceivedAtUtc { get; set; }

    /// <summary>When approved with deposit, the required non-refundable deposit amount.</summary>
    public decimal? RequiredDepositAmount { get; set; }

    public bool DepositNonRefundable { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StatusUpdatedAtUtc { get; set; }

    /// <summary>JSON audit of legal document acceptance at booking request (see LegalAcceptanceRecord).</summary>
    public string? BookingLegalAcceptanceJson { get; set; }
}
