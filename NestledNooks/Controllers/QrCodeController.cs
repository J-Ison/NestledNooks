using Microsoft.AspNetCore.Mvc;
using NestledNooks.Services;

namespace NestledNooks.Controllers;

[ApiController]
[Route("api/qrcode")]
public sealed class QrCodeController(IQrCodeService qrCodeService, IGuestWifiService guestWifi) : ControllerBase
{
    [HttpGet("main.png")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> MainPng(CancellationToken cancellationToken)
    {
        var url = await qrCodeService.GetMainUrlAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
            return NotFound("No main QR code URL has been saved yet.");

        var png = qrCodeService.GeneratePng(url);
        return File(png, "image/png", "nestled-nooks-main-qrcode.png");
    }

    [HttpGet("deerfield-retreat-guide.png")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> DeerfieldGuestGuidePng(CancellationToken cancellationToken)
    {
        var url = await qrCodeService.GetDeerfieldGuestGuideUrlAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(url))
            return NotFound("No Deerfield Retreat guest guide QR URL has been saved yet.");

        var png = qrCodeService.GeneratePng(url);
        return File(png, "image/png", "deerfield-retreat-guest-guide-qrcode.png");
    }

    [HttpGet("deerfield-retreat-wifi.png")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public IActionResult DeerfieldRetreatWifiPng()
    {
        var payload = guestWifi.GetDeerfieldRetreatWifiQrPayload();
        if (string.IsNullOrWhiteSpace(payload))
            return NotFound("Deerfield Retreat WiFi is not configured.");

        var png = guestWifi.GenerateDeerfieldRetreatWifiQrPng();
        return File(png, "image/png", "deerfield-retreat-wifi-qrcode.png");
    }
}
