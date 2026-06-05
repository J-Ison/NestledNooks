namespace NestledNooks.Services;

public static class WifiQrEncoding
{
    /// <summary>
    /// Builds a WiFi QR payload (WIFI:T:...;S:...;P:...;;) per the common WiFi QR format.
    /// </summary>
    public static string BuildPayload(string ssid, string password, string authType = "WPA")
    {
        if (string.IsNullOrWhiteSpace(ssid))
            throw new ArgumentException("SSID is required.", nameof(ssid));

        var normalizedAuth = string.IsNullOrWhiteSpace(authType)
            ? "WPA"
            : authType.Trim().ToUpperInvariant();

        if (normalizedAuth is not ("WPA" or "WEP" or "NOPASS"))
            throw new ArgumentException("Auth type must be WPA, WEP, or nopass.", nameof(authType));

        var escapedSsid = EscapeField(ssid.Trim());
        var escapedPassword = EscapeField(password ?? "");

        return normalizedAuth == "NOPASS"
            ? $"WIFI:T:nopass;S:{escapedSsid};;"
            : $"WIFI:T:{normalizedAuth};S:{escapedSsid};P:{escapedPassword};;";
    }

    private static string EscapeField(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace(";", "\\;", StringComparison.Ordinal)
            .Replace(",", "\\,", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
