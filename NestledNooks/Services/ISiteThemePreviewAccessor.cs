using NestledNooks.Data;

namespace NestledNooks.Services;

public interface ISiteThemePreviewAccessor
{
    Task<SiteTheme?> GetActivePreviewAsync(CancellationToken cancellationToken = default);
    Task<SiteThemePreviewStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task SetPreviewAsync(SiteTheme theme, TimeSpan duration, CancellationToken cancellationToken = default);
    Task ClearPreviewAsync(CancellationToken cancellationToken = default);
}

public sealed record SiteThemePreviewStatus(bool IsActive, DateTime? ExpiresUtc)
{
    public TimeSpan? Remaining =>
        IsActive && ExpiresUtc is { } exp
            ? exp > DateTime.UtcNow ? exp - DateTime.UtcNow : TimeSpan.Zero
            : null;
}
