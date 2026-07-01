using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed record SiteSettingsSnapshot(bool DirectBookingsEnabled)
{
    public static SiteSettingsSnapshot FromEntity(SiteSettings? row) =>
        row is null ? Defaults() : new SiteSettingsSnapshot(row.DirectBookingsEnabled);

    public static SiteSettingsSnapshot Defaults() => new(DirectBookingsEnabled: true);
}

public sealed record SiteSettingsSaveResult(bool Succeeded, string? ErrorMessage);

public interface ISiteSettingsService
{
    Task<SiteSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<SiteSettingsSaveResult> SetDirectBookingsEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default);
}

public sealed class SiteSettingsService(
    ApplicationDbContext db,
    IHttpContextAccessor httpContextAccessor,
    IMemoryCache cache,
    IOptions<GuestFacingCacheOptions> cacheOptions) : ISiteSettingsService
{
    private const int SettingsRowId = 1;

    private TimeSpan CacheDuration =>
        TimeSpan.FromMinutes(Math.Max(1, cacheOptions.Value.SiteSettingsMinutes));

    public async Task<SiteSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            GuestDataCacheKeys.SiteSettings,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var row = await db.SiteSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
                    .ConfigureAwait(false);

                return SiteSettingsSnapshot.FromEntity(row);
            }) ?? SiteSettingsSnapshot.Defaults();
    }

    public async Task<SiteSettingsSaveResult> SetDirectBookingsEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (!HostStaffAuthorization.IsOwner(httpContextAccessor.HttpContext?.User))
            return new SiteSettingsSaveResult(false, "Owner access is required.");

        var row = await GetOrCreateRowAsync(cancellationToken).ConfigureAwait(false);
        row.DirectBookingsEnabled = enabled;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        cache.Remove(GuestDataCacheKeys.SiteSettings);
        return new SiteSettingsSaveResult(true, null);
    }

    private async Task<SiteSettings> GetOrCreateRowAsync(CancellationToken cancellationToken)
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
}
