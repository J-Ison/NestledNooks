namespace NestledNooks.Data;

/// <summary>Single-row site-wide settings (Id is always 1).</summary>
public class SiteSettings
{
    public int Id { get; set; } = 1;

    /// <summary>Canonical URL encoded in the main marketing QR code.</summary>
    public string? MainQrCodeUrl { get; set; }

    /// <summary>QR link for the Deerfield Retreat guest guide (print in house / share with guests).</summary>
    public string? DeerfieldGuestGuideQrCodeUrl { get; set; }

    /// <summary>Prepended to every guest email from Manage bookings (supports {{tokens}}).</summary>
    public string? GuestEmailHeaderTemplate { get; set; }

    /// <summary>Appended to every guest email from Manage bookings (supports {{tokens}}).</summary>
    public string? GuestEmailFooterTemplate { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
