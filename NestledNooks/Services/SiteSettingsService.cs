using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed record SiteSettingsSnapshot(bool DirectBookingsEnabled);

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
    IHttpContextAccessor httpContextAccessor) : ISiteSettingsService
{
    private const int SettingsRowId = 1;

    public async Task<SiteSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        var row = await db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        return new SiteSettingsSnapshot(row?.DirectBookingsEnabled ?? true);
    }

    public async Task<SiteSettingsSaveResult> SetDirectBookingsEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken = default)
    {
        if (!HostStaffAuthorization.IsOwner(httpContextAccessor.HttpContext?.User))
            return new SiteSettingsSaveResult(false, "Owner access is required.");

        var row = await db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            row = new SiteSettings { Id = SettingsRowId, DirectBookingsEnabled = enabled };
            db.SiteSettings.Add(row);
        }
        else
        {
            row.DirectBookingsEnabled = enabled;
        }

        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new SiteSettingsSaveResult(true, null);
    }
}
