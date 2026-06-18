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

    /// <summary>Standard check-in time (24h), e.g. 16:00 for 4:00 PM.</summary>
    public string CheckInTime { get; set; } = PropertyStayTimes.DefaultCheckInTime;

    /// <summary>Standard check-out time (24h), e.g. 10:00 for 10:00 AM.</summary>
    public string CheckOutTime { get; set; } = PropertyStayTimes.DefaultCheckOutTime;

    /// <summary>Airbnb calendar export (.ics) URL — import only.</summary>
    public string? AirbnbIcalUrl { get; set; }

    /// <summary>Vrbo calendar export (.ics) URL — import only.</summary>
    public string? VrboIcalUrl { get; set; }

    /// <summary>PriceLabs listing ID for dynamic nightly rates (Customer API).</summary>
    public string? PriceLabsListingId { get; set; }

    /// <summary>PriceLabs PMS name for the listing (e.g. airbnb, vrbo). Auto-detected when omitted.</summary>
    public string? PriceLabsPms { get; set; }

    /// <summary>PriceLabs listing ID for Airbnb channel estimates (falls back to auto-detect by PMS/name).</summary>
    public string? PriceLabsAirbnbListingId { get; set; }

    /// <summary>PriceLabs listing ID for Vrbo channel estimates (falls back to auto-detect by PMS/name).</summary>
    public string? PriceLabsVrboListingId { get; set; }

    /// <summary>Cleaning fee shown on Airbnb (defaults to property cleaning fee when omitted).</summary>
    public decimal? AirbnbCleaningFee { get; set; }

    /// <summary>Cleaning fee shown on Vrbo (defaults to property cleaning fee when omitted).</summary>
    public decimal? VrboCleaningFee { get; set; }

    /// <summary>Estimated Airbnb guest service fee percent applied to rent + cleaning.</summary>
    public decimal? AirbnbGuestServiceFeePercent { get; set; }

    /// <summary>Estimated Vrbo guest service fee percent applied to rent + cleaning.</summary>
    public decimal? VrboGuestServiceFeePercent { get; set; }

    /// <summary>Estimated Vrbo occupancy/lodging tax percent applied after service fee.</summary>
    public decimal? VrboOccupancyTaxPercent { get; set; }
}
