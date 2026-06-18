namespace NestledNooks.Services;

public sealed class PriceLabsOptions
{
    public const string SectionName = "PriceLabs";

    /// <summary>Enable PriceLabs API sync and dynamic pricing on direct bookings.</summary>
    public bool Enabled { get; set; }

    /// <summary>Customer API key from PriceLabs account settings → API Details.</summary>
    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.pricelabs.co/v1";

    /// <summary>Minutes between automatic price syncs.</summary>
    public int SyncIntervalMinutes { get; set; } = 360;

    /// <summary>How many days ahead to pull from PriceLabs (max ~540).</summary>
    public int SyncDaysAhead { get; set; } = 540;

    /// <summary>Default Airbnb guest service fee percent when not set per property.</summary>
    public decimal DefaultAirbnbGuestServiceFeePercent { get; set; } = 14.2m;

    /// <summary>Default Vrbo guest service fee percent when not set per property.</summary>
    public decimal DefaultVrboGuestServiceFeePercent { get; set; } = 8m;
}
