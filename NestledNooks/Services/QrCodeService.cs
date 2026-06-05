using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;
using QRCoder;

namespace NestledNooks.Services;

public sealed class QrCodeService(ApplicationDbContext db, ILogger<QrCodeService> logger) : IQrCodeService
{
    private const int SettingsRowId = 1;
    private const int MaxUrlLength = 500;

    public static readonly string DefaultMainQrCodeUrl = "https://blackhillsnestlednooks.com/";

    public static readonly string DefaultDeerfieldGuestGuideQrCodeUrl =
        "https://blackhillsnestlednooks.com/properties/deerfield-retreat/guide";

    public QrCodeUrlResult NormalizeUrl(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return QrCodeUrlResult.Fail("Enter a website URL.");

        var trimmed = raw.Trim();
        if (trimmed.Length > MaxUrlLength)
            return QrCodeUrlResult.Fail($"URL cannot be longer than {MaxUrlLength} characters.");

        var candidate = trimmed.Contains("://", StringComparison.Ordinal)
            ? trimmed
            : $"https://{trimmed}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
            return QrCodeUrlResult.Fail("Enter a valid website URL (for example, https://blackhillsnestlednooks.com/).");

        if (uri.Scheme is not ("http" or "https"))
            return QrCodeUrlResult.Fail("Only http and https URLs are supported.");

        if (string.IsNullOrWhiteSpace(uri.Host))
            return QrCodeUrlResult.Fail("Enter a valid website URL.");

        return QrCodeUrlResult.Ok(uri.ToString());
    }

    public byte[] GeneratePng(string normalizedUrl, int pixelsPerModule = 20)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(normalizedUrl, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return png.GetGraphic(Math.Clamp(pixelsPerModule, 4, 40));
    }

    public string ToDataUrl(byte[] pngBytes) =>
        $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";

    public async Task<SiteQrSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        return row is null
            ? new SiteQrSettings()
            : new SiteQrSettings
            {
                MainQrCodeUrl = row.MainQrCodeUrl,
                DeerfieldGuestGuideQrCodeUrl = row.DeerfieldGuestGuideQrCodeUrl,
                UpdatedAtUtc = row.UpdatedAtUtc,
            };
    }

    public async Task<string?> GetMainUrlAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(settings.MainQrCodeUrl) ? null : settings.MainQrCodeUrl;
    }

    public async Task<string?> GetDeerfieldGuestGuideUrlAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(settings.DeerfieldGuestGuideQrCodeUrl)
            ? null
            : settings.DeerfieldGuestGuideQrCodeUrl;
    }

    public async Task<QrCodeSaveResult> SaveMainUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url);
        if (!normalized.Succeeded || normalized.NormalizedUrl is null)
            return QrCodeSaveResult.Fail(normalized.ErrorMessage ?? "Invalid URL.");

        var row = await GetOrCreateSettingsRowAsync(cancellationToken).ConfigureAwait(false);
        row.MainQrCodeUrl = normalized.NormalizedUrl;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Main QR code URL saved: {Url}", normalized.NormalizedUrl);
        return QrCodeSaveResult.Ok(normalized.NormalizedUrl);
    }

    public async Task<QrCodeSaveResult> SaveDeerfieldGuestGuideUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeUrl(url);
        if (!normalized.Succeeded || normalized.NormalizedUrl is null)
            return QrCodeSaveResult.Fail(normalized.ErrorMessage ?? "Invalid URL.");

        var row = await GetOrCreateSettingsRowAsync(cancellationToken).ConfigureAwait(false);
        row.DeerfieldGuestGuideQrCodeUrl = normalized.NormalizedUrl;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deerfield guest guide QR URL saved: {Url}", normalized.NormalizedUrl);
        return QrCodeSaveResult.Ok(normalized.NormalizedUrl);
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            row = new SiteSettings
            {
                Id = SettingsRowId,
                MainQrCodeUrl = DefaultMainQrCodeUrl,
                DeerfieldGuestGuideQrCodeUrl = DefaultDeerfieldGuestGuideQrCodeUrl,
                UpdatedAtUtc = DateTime.UtcNow,
            };
            db.SiteSettings.Add(row);
            logger.LogInformation(
                "Seeded QR settings (main: {MainUrl}, guest guide: {GuideUrl})",
                DefaultMainQrCodeUrl,
                DefaultDeerfieldGuestGuideQrCodeUrl);
        }
        else
        {
            var changed = false;
            if (string.IsNullOrWhiteSpace(row.MainQrCodeUrl))
            {
                row.MainQrCodeUrl = DefaultMainQrCodeUrl;
                changed = true;
            }
            else
            {
                var fixedMain = FixLegacyQrUrl(row.MainQrCodeUrl);
                if (!string.Equals(fixedMain, row.MainQrCodeUrl, StringComparison.Ordinal))
                {
                    row.MainQrCodeUrl = fixedMain;
                    changed = true;
                }
            }

            if (string.IsNullOrWhiteSpace(row.DeerfieldGuestGuideQrCodeUrl))
            {
                row.DeerfieldGuestGuideQrCodeUrl = DefaultDeerfieldGuestGuideQrCodeUrl;
                changed = true;
            }
            else
            {
                var fixedGuide = FixLegacyQrUrl(row.DeerfieldGuestGuideQrCodeUrl);
                if (!string.Equals(fixedGuide, row.DeerfieldGuestGuideQrCodeUrl, StringComparison.Ordinal))
                {
                    row.DeerfieldGuestGuideQrCodeUrl = fixedGuide;
                    changed = true;
                }
            }

            if (!changed)
                return;

            row.UpdatedAtUtc = DateTime.UtcNow;
            logger.LogInformation("Backfilled or corrected QR code URLs on SiteSettings.");
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<SiteSettings> GetOrCreateSettingsRowAsync(CancellationToken cancellationToken)
    {
        var row = await db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        if (row is not null)
            return row;

        row = new SiteSettings { Id = SettingsRowId };
        db.SiteSettings.Add(row);
        return row;
    }

    /// <summary>Corrects the old backhillsnestlednooks.com typo and upgrades http to https for our domain.</summary>
    public static string FixLegacyQrUrl(string url)
    {
        var fixedUrl = url.Replace(
            "backhillsnestlednooks.com",
            "blackhillsnestlednooks.com",
            StringComparison.OrdinalIgnoreCase);

        if (fixedUrl.StartsWith("http://blackhillsnestlednooks.com", StringComparison.OrdinalIgnoreCase))
            fixedUrl = "https://" + fixedUrl["http://".Length..];

        return fixedUrl;
    }
}
