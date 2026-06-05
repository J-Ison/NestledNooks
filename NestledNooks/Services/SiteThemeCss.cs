using System.Globalization;
using System.Text;
using NestledNooks.Data;

namespace NestledNooks.Services;

public static class SiteThemeCss
{
    public static string BuildStyleBlock(SiteTheme theme)
    {
        var dark = IsDark(theme.PageBgTop);
        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        AppendVar(sb, "--nn-primary", theme.PrimaryColor);
        AppendVar(sb, "--nn-primary-light", theme.PrimaryLightColor);
        AppendVar(sb, "--nn-primary-soft", theme.PrimarySoftBg);
        AppendVar(sb, "--nn-primary-border", theme.PrimaryBorderColor);
        AppendVar(sb, "--nn-primary-text", theme.PrimaryTextColor);
        AppendVar(sb, "--nn-accent", theme.AccentColor);
        AppendVar(sb, "--nn-accent-border", theme.AccentBorderColor);
        AppendVar(sb, "--nn-hero-start", theme.HeroStartColor);
        AppendVar(sb, "--nn-hero-mid", theme.HeroMidColor);
        AppendVar(sb, "--nn-hero-end", theme.HeroEndColor);
        AppendVar(sb, "--nn-hero-border", theme.HeroBorderColor);
        AppendVar(sb, "--nn-booking", theme.BookingColor);
        AppendVar(sb, "--nn-booking-dark", theme.BookingDarkColor);
        AppendVar(sb, "--nn-page-bg-top", theme.PageBgTop);
        AppendVar(sb, "--nn-page-bg-bottom", theme.PageBgBottom);
        AppendVar(sb, "--nn-page-bg-deep", theme.PageBgBottom);
        AppendVar(sb, "--nn-page-bg-fade", dark ? theme.PageBgBottom : "#ffffff");
        AppendVar(sb, "--nn-surface", dark ? "#334155" : "#ffffff");
        AppendVar(sb, "--nn-surface-muted", dark ? theme.PrimarySoftBg : "#f8fafc");
        AppendVar(sb, "--nn-ink", dark ? "#f1f5f9" : "#0f172a");
        AppendVar(sb, "--nn-ink-secondary", dark ? "#cbd5e1" : "#334155");
        AppendVar(sb, "--nn-ink-muted", dark ? "#94a3b8" : "#64748b");
        AppendVar(sb, "--nn-line", dark ? theme.PrimaryBorderColor : "#e2e8f0");
        AppendVar(sb, "--nn-hero-text", dark ? "#f8fafc" : "#0f172a");
        AppendVar(sb, "--nn-hero-lead", dark ? "#cbd5e1" : "#1e293b");
        AppendVar(sb, "--nn-appbar-text", dark ? "#f8fafc" : "#0f172a");
        AppendVar(sb, "--nn-shadow-soft", dark ? "0 6px 20px rgba(0, 0, 0, 0.35)" : "0 6px 20px rgba(15, 23, 42, 0.08)");
        AppendVar(sb, "--nn-input-bg", dark ? "#0f172a" : "#ffffff");
        AppendVar(sb, "--nn-input-text", dark ? "#f1f5f9" : "#0f172a");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static bool IsDark(string hex)
    {
        if (!TryParseHex(hex, out var r, out var g, out var b))
            return false;

        var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255d;
        return luminance < 0.42;
    }

    private static bool TryParseHex(string hex, out int r, out int g, out int b)
    {
        r = g = b = 0;
        if (string.IsNullOrWhiteSpace(hex))
            return false;

        hex = hex.Trim().TrimStart('#');
        if (hex.Length is not (3 or 6))
            return false;

        if (hex.Length == 3)
            hex = string.Concat(hex.Select(c => $"{c}{c}"));

        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return false;

        r = (rgb >> 16) & 0xFF;
        g = (rgb >> 8) & 0xFF;
        b = rgb & 0xFF;
        return true;
    }

    private static void AppendVar(StringBuilder sb, string name, string value) =>
        sb.Append("  ").Append(name).Append(": ").Append(value).AppendLine(";");
}
