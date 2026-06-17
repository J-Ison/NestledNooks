namespace NestledNooks.Data;

/// <summary>Guest-facing listing content for a bookable property.</summary>
public class RentalProperty
{
    public int Id { get; set; }

    /// <summary>URL slug, e.g. deerfield-retreat.</summary>
    public string Slug { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public bool IsPublished { get; set; } = true;

    public bool IsHomepage { get; set; }

    public int SortOrder { get; set; }

    public string MetaDescription { get; set; } = "";

    public string Subtitle { get; set; } = "";

    /// <summary>Short highlight pills, e.g. "Sleeps 12". JSON string array.</summary>
    public string StatsJson { get; set; } = "[]";

    /// <summary>Intro line 1, e.g. "Entire home · Black Hills, South Dakota".</summary>
    public string TagsLine1 { get; set; } = "";

    /// <summary>Intro line 2, e.g. "Family-friendly · Prairie views".</summary>
    public string TagsLine2 { get; set; } = "";

    /// <summary>JSON array of { title, subtitle } badge objects.</summary>
    public string BadgesJson { get; set; } = "[]";

    public string AboutText { get; set; } = "";

    /// <summary>JSON string array of amenity lines.</summary>
    public string AmenitiesJson { get; set; } = "[]";

    public string LocationText { get; set; } = "";

    public string GuideTeaserText { get; set; } = "";

    public string BookingSubtext { get; set; } = "";

    public string BookingFinePrint { get; set; } = "";

    public string? AirbnbUrl { get; set; }

    public string? VrboUrl { get; set; }

    /// <summary>One-time cleaning fee for direct bookings (USD).</summary>
    public decimal CleaningFee { get; set; } = 200m;

    public int MinimumNights { get; set; } = 2;

    /// <summary>Earliest check-in is today + this many days (e.g. 10 = book at least 10 days out).</summary>
    public int MinAdvanceBookingDays { get; set; } = 10;

    /// <summary>Latest check-in is today + this many days (e.g. 365 = one year ahead).</summary>
    public int MaxBookingDaysAhead { get; set; } = 365;

    /// <summary>Pet deposit charged per pair of pets (1–2 pets = one deposit, 3–4 = two, etc.).</summary>
    public decimal PetDepositPerTwoPets { get; set; } = 50m;

    /// <summary>Only apply Airbnb/Vrbo iCal blocks through today + this many days (0 = ignore external calendar).</summary>
    public int ExternalCalendarTrustDays { get; set; } = 180;

    /// <summary>When true, guests may request stays beyond the calendar trust window (manual confirmation).</summary>
    public bool AllowFarAdvanceDirectBooking { get; set; } = true;

    /// <summary>JSON array of { url, alt } photo objects.</summary>
    public string PhotosJson { get; set; } = "[]";

    public DateTime UpdatedAtUtc { get; set; }
}
