namespace NestledNooks.Data;

/// <summary>Single-row site-wide color theme (Id is always 1).</summary>
public class SiteTheme
{
    public int Id { get; set; } = 1;

    public string PresetKey { get; set; } = "default";

    public string PrimaryColor { get; set; } = "#2563eb";
    public string PrimaryLightColor { get; set; } = "#3b82f6";
    public string PrimarySoftBg { get; set; } = "#eff6ff";
    public string PrimaryBorderColor { get; set; } = "#bfdbfe";
    public string PrimaryTextColor { get; set; } = "#1e40af";

    public string AccentColor { get; set; } = "#93c5fd";
    public string AccentBorderColor { get; set; } = "#dbeafe";

    public string HeroStartColor { get; set; } = "#93c5fd";
    public string HeroMidColor { get; set; } = "#60a5fa";
    public string HeroEndColor { get; set; } = "#34d399";
    public string HeroBorderColor { get; set; } = "#2563eb";

    public string BookingColor { get; set; } = "#059669";
    public string BookingDarkColor { get; set; } = "#047857";

    public string PageBgTop { get; set; } = "#f0f9ff";
    public string PageBgBottom { get; set; } = "#f8fafc";

    public DateTime UpdatedAtUtc { get; set; }
}
