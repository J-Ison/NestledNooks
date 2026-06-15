using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed record GuestEmailWrapperSettings(
    string HeaderTemplate,
    string FooterTemplate);

public sealed record GuestEmailWrapperSaveResult(bool Succeeded, string? ErrorMessage);

public interface IGuestEmailWrapperService
{
    Task<GuestEmailWrapperSettings> GetAsync(CancellationToken cancellationToken = default);

    Task<GuestEmailWrapperSaveResult> SaveAsync(
        string headerTemplate,
        string footerTemplate,
        CancellationToken cancellationToken = default);

    Task<string> ComposeFullBodyAsync(
        string guestMessage,
        BookingGuestMessageEmailPayload payload,
        CancellationToken cancellationToken = default);

    string ComposeFullBody(
        string guestMessage,
        BookingGuestMessageEmailPayload payload,
        GuestEmailWrapperSettings settings);
}

public sealed class GuestEmailWrapperService(
    ApplicationDbContext db,
    IGuestEmailTemplateService templates,
    IHttpContextAccessor httpContextAccessor) : IGuestEmailWrapperService
{
    private const int SettingsRowId = 1;

    public async Task<GuestEmailWrapperSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var row = await db.SiteSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        return new GuestEmailWrapperSettings(
            string.IsNullOrWhiteSpace(row?.GuestEmailHeaderTemplate)
                ? GuestEmailWrapperDefaults.Header
                : row!.GuestEmailHeaderTemplate!,
            string.IsNullOrWhiteSpace(row?.GuestEmailFooterTemplate)
                ? GuestEmailWrapperDefaults.Footer
                : row!.GuestEmailFooterTemplate!);
    }

    public async Task<GuestEmailWrapperSaveResult> SaveAsync(
        string headerTemplate,
        string footerTemplate,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        headerTemplate = headerTemplate.Trim();
        footerTemplate = footerTemplate.Trim();

        if (string.IsNullOrWhiteSpace(headerTemplate))
            return new GuestEmailWrapperSaveResult(false, "Header cannot be empty.");
        if (string.IsNullOrWhiteSpace(footerTemplate))
            return new GuestEmailWrapperSaveResult(false, "Footer cannot be empty.");

        if (headerTemplate.Length > 2000 || footerTemplate.Length > 4000)
            return new GuestEmailWrapperSaveResult(false, "Header or footer is too long.");

        var row = await db.SiteSettings
            .FirstOrDefaultAsync(s => s.Id == SettingsRowId, cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            row = new SiteSettings { Id = SettingsRowId };
            db.SiteSettings.Add(row);
        }

        row.GuestEmailHeaderTemplate = headerTemplate;
        row.GuestEmailFooterTemplate = footerTemplate;
        row.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new GuestEmailWrapperSaveResult(true, null);
    }

    public async Task<string> ComposeFullBodyAsync(
        string guestMessage,
        BookingGuestMessageEmailPayload payload,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken).ConfigureAwait(false);
        return ComposeFullBody(guestMessage, payload, settings);
    }

    public string ComposeFullBody(
        string guestMessage,
        BookingGuestMessageEmailPayload payload,
        GuestEmailWrapperSettings settings) =>
        ComposeFullBodyCore(guestMessage, payload, settings);

    private string ComposeFullBodyCore(
        string guestMessage,
        BookingGuestMessageEmailPayload payload,
        GuestEmailWrapperSettings settings)
    {
        var context = new GuestEmailRenderContext(
            payload.GuestFullName,
            payload.GuestEmail,
            payload.BookingNumber,
            payload.PropertyDisplayName,
            payload.CheckIn,
            payload.CheckOut,
            payload.NightCount,
            payload.TotalAmount,
            payload.PayUrl);

        var header = templates.Render(settings.HeaderTemplate, context).TrimEnd();
        var body = guestMessage.Trim();
        var footer = templates.Render(settings.FooterTemplate, context).TrimStart();

        return $"{header}\r\n\r\n{body}\r\n\r\n{footer}";
    }
}
