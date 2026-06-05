using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class SiteThemeService(
    ApplicationDbContext db,
    SiteThemeCache cache,
    ISiteThemePreviewAccessor previewAccessor) : ISiteThemeService
{
    private const int ThemeRowId = 1;

    public TimeSpan PreviewDuration { get; } = TimeSpan.FromMinutes(1);

    public async Task<string> GetEffectiveCssAsync(CancellationToken cancellationToken = default)
    {
        var theme = await GetEffectiveAsync(cancellationToken).ConfigureAwait(false);
        return SiteThemeCss.BuildStyleBlock(theme);
    }

    public async Task<SiteTheme> GetEffectiveAsync(CancellationToken cancellationToken = default)
    {
        var preview = await previewAccessor.GetActivePreviewAsync(cancellationToken).ConfigureAwait(false);
        if (preview is not null)
            return preview;

        return await GetCurrentAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<SiteThemePreviewStatus> GetPreviewStatusAsync(CancellationToken cancellationToken = default) =>
        previewAccessor.GetStatusAsync(cancellationToken);

    public async Task<SiteTheme> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (cache.IsLoaded)
            return cache.Current;

        await ReloadFromDatabaseAsync(cancellationToken).ConfigureAwait(false);
        return cache.Current;
    }

    public async Task StartPreviewAsync(SiteTheme theme, CancellationToken cancellationToken = default)
    {
        await previewAccessor.SetPreviewAsync(theme, PreviewDuration, cancellationToken).ConfigureAwait(false);
    }

    public Task ClearPreviewAsync(CancellationToken cancellationToken = default) =>
        previewAccessor.ClearPreviewAsync(cancellationToken);

    public async Task<SiteTheme> SaveAsync(SiteTheme theme, CancellationToken cancellationToken = default)
    {
        theme.Id = ThemeRowId;
        theme.UpdatedAtUtc = DateTime.UtcNow;

        var existing = await db.SiteThemes.FindAsync([ThemeRowId], cancellationToken).ConfigureAwait(false);
        if (existing is null)
            db.SiteThemes.Add(theme);
        else
            db.Entry(existing).CurrentValues.SetValues(theme);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        cache.Set(theme);
        await ClearPreviewAsync(cancellationToken).ConfigureAwait(false);
        return theme;
    }

    public async Task<SiteTheme> ResetToDefaultAsync(CancellationToken cancellationToken = default)
    {
        var theme = SiteThemePresets.CreateTheme(SiteThemePresets.Default);
        return await SaveAsync(theme, cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        if (await db.SiteThemes.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            await ReloadFromDatabaseAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var theme = SiteThemePresets.CreateTheme(SiteThemePresets.Default);
        theme.UpdatedAtUtc = DateTime.UtcNow;
        db.SiteThemes.Add(theme);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        cache.Set(theme);
    }

    private async Task ReloadFromDatabaseAsync(CancellationToken cancellationToken)
    {
        var theme = await db.SiteThemes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ThemeRowId, cancellationToken)
            .ConfigureAwait(false);

        cache.Set(theme ?? SiteThemePresets.CreateTheme(SiteThemePresets.Default));
    }
}

/// <summary>In-memory copy of the saved (database) theme for fast reads.</summary>
public sealed class SiteThemeCache
{
    private SiteTheme _current = SiteThemePresets.CreateTheme(SiteThemePresets.Default);

    public bool IsLoaded { get; private set; }

    public SiteTheme Current => _current;

    public void Set(SiteTheme theme)
    {
        _current = theme;
        IsLoaded = true;
    }
}
