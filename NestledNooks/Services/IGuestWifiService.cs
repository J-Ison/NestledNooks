namespace NestledNooks.Services;

public interface IGuestWifiService
{
    const string DeerfieldRetreatWifiQrCodePublicPath = "/api/qrcode/deerfield-retreat-wifi.png";

    GuestWifiPropertyOptions GetDeerfieldRetreatSettings();

    string? GetDeerfieldRetreatWifiQrPayload();

    byte[] GenerateDeerfieldRetreatWifiQrPng(int pixelsPerModule = 20);
}
