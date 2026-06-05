using NestledNooks.Data;

namespace NestledNooks.Services;

public interface ISiteThemeService
{
    TimeSpan PreviewDuration { get; }

    Task<SiteTheme> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<SiteTheme> GetEffectiveAsync(CancellationToken cancellationToken = default);
    Task<SiteThemePreviewStatus> GetPreviewStatusAsync(CancellationToken cancellationToken = default);
    Task<SiteTheme> SaveAsync(SiteTheme theme, CancellationToken cancellationToken = default);
    Task<SiteTheme> ResetToDefaultAsync(CancellationToken cancellationToken = default);
    Task StartPreviewAsync(SiteTheme theme, CancellationToken cancellationToken = default);
    Task ClearPreviewAsync(CancellationToken cancellationToken = default);
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);
    Task<string> GetEffectiveCssAsync(CancellationToken cancellationToken = default);
}
