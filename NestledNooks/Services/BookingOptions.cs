namespace NestledNooks.Services;

public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    /// <summary>Minutes between automatic iCal sync from Airbnb/Vrbo export URLs.</summary>
    public int CalendarSyncIntervalMinutes { get; set; } = 30;

    public List<PropertyBookingOptions> Properties { get; set; } = [];
}

public sealed class PropertyBookingOptions
{
    public string Slug { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public decimal NightlyRate { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal PetFeePerStay { get; set; }
    public int MaxGuests { get; set; } = 12;
    public int MaxPets { get; set; } = 4;
    public int MinimumNights { get; set; } = 2;

    /// <summary>Airbnb calendar export (.ics) URL — import only.</summary>
    public string? AirbnbIcalUrl { get; set; }

    /// <summary>Vrbo calendar export (.ics) URL — import only.</summary>
    public string? VrboIcalUrl { get; set; }
}
