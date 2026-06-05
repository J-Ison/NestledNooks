namespace NestledNooks.Services;

public sealed class GuestWifiOptions
{
    public const string SectionName = "GuestWifi";

    public GuestWifiPropertyOptions DeerfieldRetreat { get; set; } = new();
}

public sealed class GuestWifiPropertyOptions
{
    public string Ssid { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>WPA, WEP, or nopass — used when building WiFi QR payloads.</summary>
    public string AuthType { get; set; } = "WPA";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Ssid) && !string.IsNullOrWhiteSpace(Password);
}
