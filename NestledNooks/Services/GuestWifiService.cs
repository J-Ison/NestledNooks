using Microsoft.Extensions.Options;

namespace NestledNooks.Services;

public sealed class GuestWifiService(
    IOptions<GuestWifiOptions> options,
    IQrCodeService qrCodes) : IGuestWifiService
{
    public GuestWifiPropertyOptions GetDeerfieldRetreatSettings() =>
        options.Value.DeerfieldRetreat;

    public string? GetDeerfieldRetreatWifiQrPayload()
    {
        var settings = GetDeerfieldRetreatSettings();
        if (!settings.IsConfigured)
            return null;

        return WifiQrEncoding.BuildPayload(
            settings.Ssid,
            settings.Password,
            settings.AuthType);
    }

    public byte[] GenerateDeerfieldRetreatWifiQrPng(int pixelsPerModule = 20)
    {
        var payload = GetDeerfieldRetreatWifiQrPayload()
            ?? throw new InvalidOperationException("Deerfield Retreat WiFi is not configured.");

        return qrCodes.GeneratePng(payload, pixelsPerModule);
    }
}
