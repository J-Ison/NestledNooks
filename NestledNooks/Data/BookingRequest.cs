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

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StatusUpdatedAtUtc { get; set; }
}
