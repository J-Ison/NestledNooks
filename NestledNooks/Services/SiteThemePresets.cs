using NestledNooks.Data;

namespace NestledNooks.Services;

public static class SiteThemePresets
{
    public const string Default = "default";
    public const string Forest = "forest";
    public const string Sunset = "sunset";
    public const string Slate = "slate";
    public const string Professional = "professional";
    public const string Grayscale = "grayscale";
    public const string Dark = "dark";
    public const string Ocean = "ocean";
    public const string Rustic = "rustic";
    public const string Wine = "wine";

    public static IReadOnlyList<SiteThemePresetInfo> All { get; } =
    [
        new(Default, "Sky & meadow", "Classic blue sky with green accents"),
        new(Forest, "Forest retreat", "Deep greens and natural tones"),
        new(Sunset, "Warm sunset", "Coral and amber warmth"),
        new(Slate, "Mountain slate", "Cool slate and indigo"),
        new(Professional, "Professional", "Navy, steel, and clean neutrals"),
        new(Grayscale, "Grayscale", "Monochrome charcoal and silver"),
        new(Dark, "Dark mode", "Dark surfaces with cool blue highlights"),
        new(Ocean, "Ocean", "Teal and deep sea blues"),
        new(Rustic, "Rustic lodge", "Warm browns and cabin tones"),
        new(Wine, "Wine country", "Burgundy and soft rose"),
    ];

    public static SiteTheme CreateTheme(string presetKey) =>
        presetKey switch
        {
            Forest => new SiteTheme
            {
                PresetKey = Forest,
                PrimaryColor = "#166534",
                PrimaryLightColor = "#22c55e",
                PrimarySoftBg = "#ecfdf5",
                PrimaryBorderColor = "#86efac",
                PrimaryTextColor = "#14532d",
                AccentColor = "#86efac",
                AccentBorderColor = "#bbf7d0",
                HeroStartColor = "#86efac",
                HeroMidColor = "#4ade80",
                HeroEndColor = "#2dd4bf",
                HeroBorderColor = "#15803d",
                BookingColor = "#15803d",
                BookingDarkColor = "#166534",
                PageBgTop = "#f0fdf4",
                PageBgBottom = "#f8fafc",
            },
            Sunset => new SiteTheme
            {
                PresetKey = Sunset,
                PrimaryColor = "#c2410c",
                PrimaryLightColor = "#f97316",
                PrimarySoftBg = "#fff7ed",
                PrimaryBorderColor = "#fdba74",
                PrimaryTextColor = "#9a3412",
                AccentColor = "#fdba74",
                AccentBorderColor = "#fed7aa",
                HeroStartColor = "#fdba74",
                HeroMidColor = "#fb923c",
                HeroEndColor = "#f472b6",
                HeroBorderColor = "#ea580c",
                BookingColor = "#ea580c",
                BookingDarkColor = "#c2410c",
                PageBgTop = "#fff7ed",
                PageBgBottom = "#fffbeb",
            },
            Slate => new SiteTheme
            {
                PresetKey = Slate,
                PrimaryColor = "#4338ca",
                PrimaryLightColor = "#6366f1",
                PrimarySoftBg = "#eef2ff",
                PrimaryBorderColor = "#c7d2fe",
                PrimaryTextColor = "#3730a3",
                AccentColor = "#a5b4fc",
                AccentBorderColor = "#c7d2fe",
                HeroStartColor = "#a5b4fc",
                HeroMidColor = "#818cf8",
                HeroEndColor = "#94a3b8",
                HeroBorderColor = "#4f46e5",
                BookingColor = "#4f46e5",
                BookingDarkColor = "#4338ca",
                PageBgTop = "#f1f5f9",
                PageBgBottom = "#f8fafc",
            },
            Professional => new SiteTheme
            {
                PresetKey = Professional,
                PrimaryColor = "#1e3a5f",
                PrimaryLightColor = "#334155",
                PrimarySoftBg = "#f1f5f9",
                PrimaryBorderColor = "#cbd5e1",
                PrimaryTextColor = "#0f172a",
                AccentColor = "#94a3b8",
                AccentBorderColor = "#cbd5e1",
                HeroStartColor = "#cbd5e1",
                HeroMidColor = "#64748b",
                HeroEndColor = "#475569",
                HeroBorderColor = "#1e293b",
                BookingColor = "#1e40af",
                BookingDarkColor = "#1e3a8a",
                PageBgTop = "#f8fafc",
                PageBgBottom = "#ffffff",
            },
            Grayscale => new SiteTheme
            {
                PresetKey = Grayscale,
                PrimaryColor = "#404040",
                PrimaryLightColor = "#525252",
                PrimarySoftBg = "#f5f5f5",
                PrimaryBorderColor = "#d4d4d4",
                PrimaryTextColor = "#262626",
                AccentColor = "#a3a3a3",
                AccentBorderColor = "#d4d4d4",
                HeroStartColor = "#d4d4d4",
                HeroMidColor = "#a3a3a3",
                HeroEndColor = "#737373",
                HeroBorderColor = "#525252",
                BookingColor = "#404040",
                BookingDarkColor = "#262626",
                PageBgTop = "#fafafa",
                PageBgBottom = "#f5f5f5",
            },
            Dark => new SiteTheme
            {
                PresetKey = Dark,
                PrimaryColor = "#38bdf8",
                PrimaryLightColor = "#7dd3fc",
                PrimarySoftBg = "#1e293b",
                PrimaryBorderColor = "#334155",
                PrimaryTextColor = "#e2e8f0",
                AccentColor = "#334155",
                AccentBorderColor = "#475569",
                HeroStartColor = "#1e293b",
                HeroMidColor = "#334155",
                HeroEndColor = "#0f172a",
                HeroBorderColor = "#38bdf8",
                BookingColor = "#0ea5e9",
                BookingDarkColor = "#0284c7",
                PageBgTop = "#0f172a",
                PageBgBottom = "#1e293b",
            },
            Ocean => new SiteTheme
            {
                PresetKey = Ocean,
                PrimaryColor = "#0e7490",
                PrimaryLightColor = "#06b6d4",
                PrimarySoftBg = "#ecfeff",
                PrimaryBorderColor = "#a5f3fc",
                PrimaryTextColor = "#155e75",
                AccentColor = "#67e8f9",
                AccentBorderColor = "#a5f3fc",
                HeroStartColor = "#67e8f9",
                HeroMidColor = "#22d3ee",
                HeroEndColor = "#2dd4bf",
                HeroBorderColor = "#0891b2",
                BookingColor = "#0891b2",
                BookingDarkColor = "#0e7490",
                PageBgTop = "#ecfeff",
                PageBgBottom = "#f0fdfa",
            },
            Rustic => new SiteTheme
            {
                PresetKey = Rustic,
                PrimaryColor = "#92400e",
                PrimaryLightColor = "#b45309",
                PrimarySoftBg = "#fffbeb",
                PrimaryBorderColor = "#fcd34d",
                PrimaryTextColor = "#78350f",
                AccentColor = "#d6b38a",
                AccentBorderColor = "#e7d5c4",
                HeroStartColor = "#e7d5c4",
                HeroMidColor = "#c4a882",
                HeroEndColor = "#a16207",
                HeroBorderColor = "#92400e",
                BookingColor = "#b45309",
                BookingDarkColor = "#92400e",
                PageBgTop = "#fffbeb",
                PageBgBottom = "#fef3c7",
            },
            Wine => new SiteTheme
            {
                PresetKey = Wine,
                PrimaryColor = "#9f1239",
                PrimaryLightColor = "#be123c",
                PrimarySoftBg = "#fff1f2",
                PrimaryBorderColor = "#fecdd3",
                PrimaryTextColor = "#881337",
                AccentColor = "#fda4af",
                AccentBorderColor = "#fecdd3",
                HeroStartColor = "#fecdd3",
                HeroMidColor = "#fb7185",
                HeroEndColor = "#c084fc",
                HeroBorderColor = "#9f1239",
                BookingColor = "#be123c",
                BookingDarkColor = "#9f1239",
                PageBgTop = "#fff1f2",
                PageBgBottom = "#fdf2f8",
            },
            _ => new SiteTheme
            {
                PresetKey = Default,
                PrimaryColor = "#2563eb",
                PrimaryLightColor = "#3b82f6",
                PrimarySoftBg = "#eff6ff",
                PrimaryBorderColor = "#bfdbfe",
                PrimaryTextColor = "#1e40af",
                AccentColor = "#93c5fd",
                AccentBorderColor = "#dbeafe",
                HeroStartColor = "#93c5fd",
                HeroMidColor = "#60a5fa",
                HeroEndColor = "#34d399",
                HeroBorderColor = "#2563eb",
                BookingColor = "#059669",
                BookingDarkColor = "#047857",
                PageBgTop = "#f0f9ff",
                PageBgBottom = "#f8fafc",
            },
        };

    public sealed record SiteThemePresetInfo(string Key, string Title, string Description);
}
