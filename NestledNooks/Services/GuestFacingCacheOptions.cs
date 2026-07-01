namespace NestledNooks.Services;

/// <summary>Memory-cache TTLs for guest-facing reads (reduces Azure SQL load from Blazor UI).</summary>
public sealed class GuestFacingCacheOptions
{
    public const string SectionName = "GuestFacingCache";

    /// <summary>Cached <see cref="Data.RentalProperty"/> rows for listing/booking UI.</summary>
    public int PropertyMinutes { get; set; } = 15;

    /// <summary>Cached site-wide toggles (e.g. direct bookings enabled).</summary>
    public int SiteSettingsMinutes { get; set; } = 5;

    /// <summary>Cached unavailable-date sets for booking calendars.</summary>
    public int UnavailableDatesMinutes { get; set; } = 15;
}
