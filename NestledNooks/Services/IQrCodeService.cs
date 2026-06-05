namespace NestledNooks.Services;

public sealed class QrCodeUrlResult
{
    public bool Succeeded { get; init; }
    public string? NormalizedUrl { get; init; }
    public string? ErrorMessage { get; init; }

    public static QrCodeUrlResult Ok(string url) => new() { Succeeded = true, NormalizedUrl = url };

    public static QrCodeUrlResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class QrCodeSaveResult
{
    public bool Succeeded { get; init; }
    public string? SavedUrl { get; init; }
    public string? ErrorMessage { get; init; }

    public static QrCodeSaveResult Ok(string url) => new() { Succeeded = true, SavedUrl = url };

    public static QrCodeSaveResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}

public sealed class SiteQrSettings
{
    public string? MainQrCodeUrl { get; init; }
    public string? DeerfieldGuestGuideQrCodeUrl { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
}

public interface IQrCodeService
{
    const string MainQrCodePublicPath = "/api/qrcode/main.png";
    const string DeerfieldGuestGuideQrCodePublicPath = "/api/qrcode/deerfield-retreat-guide.png";

    QrCodeUrlResult NormalizeUrl(string? raw);

    byte[] GeneratePng(string normalizedUrl, int pixelsPerModule = 20);

    string ToDataUrl(byte[] pngBytes);

    Task<SiteQrSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<string?> GetMainUrlAsync(CancellationToken cancellationToken = default);

    Task<string?> GetDeerfieldGuestGuideUrlAsync(CancellationToken cancellationToken = default);

    Task<QrCodeSaveResult> SaveMainUrlAsync(string url, CancellationToken cancellationToken = default);

    Task<QrCodeSaveResult> SaveDeerfieldGuestGuideUrlAsync(string url, CancellationToken cancellationToken = default);

    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
}
