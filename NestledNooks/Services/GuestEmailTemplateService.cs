using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed record GuestEmailTemplateModel
{
    public int Id { get; set; }
    public string PropertySlug { get; set; } = "";
    public string Category { get; set; } = GuestEmailTemplateCategories.General;
    public string Title { get; set; } = "";
    public string? EmailSubject { get; set; }
    public string Body { get; set; } = "";
    public int SortOrder { get; set; }
}

public sealed record GuestEmailTemplateSaveResult(bool Succeeded, string? ErrorMessage, int? Id);

public sealed record GuestEmailRenderContext(
    string GuestFullName,
    string GuestEmail,
    string BookingNumber,
    string PropertyDisplayName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int NightCount,
    decimal TotalAmount,
    string? PayUrl);

public interface IGuestEmailTemplateService
{
    Task EnsureSeededAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuestEmailTemplateModel>> GetAllAsync(
        string? propertySlug = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GuestEmailTemplateModel>> GetForBookingAsync(
        string propertySlug,
        CancellationToken cancellationToken = default);

    Task<GuestEmailTemplateSaveResult> SaveAsync(
        GuestEmailTemplateModel model,
        CancellationToken cancellationToken = default);

    Task<GuestEmailTemplateSaveResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default);

    string Render(string template, GuestEmailRenderContext context);
}

public sealed class GuestEmailTemplateService(
    ApplicationDbContext db,
    IPropertyService propertyService,
    IHttpContextAccessor httpContextAccessor) : IGuestEmailTemplateService
{
    public async Task EnsureSeededAsync(CancellationToken cancellationToken = default)
    {
        var properties = await propertyService.GetAllAsync(cancellationToken).ConfigureAwait(false);
        foreach (var property in properties)
        {
            var slug = property.Slug.Trim().ToLowerInvariant();
            var exists = await db.GuestEmailTemplates
                .AnyAsync(t => t.PropertySlug == slug, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
                await SeedDefaultsForPropertyAsync(slug, property.DisplayName, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<GuestEmailTemplateModel>> GetAllAsync(
        string? propertySlug = null,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var query = db.GuestEmailTemplates.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(propertySlug))
        {
            var slug = propertySlug.Trim().ToLowerInvariant();
            query = query.Where(t => t.PropertySlug == slug || t.PropertySlug == "");
        }

        return await query
            .OrderBy(t => t.PropertySlug)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .Select(t => new GuestEmailTemplateModel
            {
                Id = t.Id,
                PropertySlug = t.PropertySlug,
                Category = t.Category,
                Title = t.Title,
                EmailSubject = t.EmailSubject,
                Body = t.Body,
                SortOrder = t.SortOrder,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<GuestEmailTemplateModel>> GetForBookingAsync(
        string propertySlug,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = propertySlug.Trim().ToLowerInvariant();
        return await db.GuestEmailTemplates
            .AsNoTracking()
            .Where(t => t.PropertySlug == slug || t.PropertySlug == "")
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .Select(t => new GuestEmailTemplateModel
            {
                Id = t.Id,
                PropertySlug = t.PropertySlug,
                Category = t.Category,
                Title = t.Title,
                EmailSubject = t.EmailSubject,
                Body = t.Body,
                SortOrder = t.SortOrder,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GuestEmailTemplateSaveResult> SaveAsync(
        GuestEmailTemplateModel model,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var title = model.Title.Trim();
        var body = model.Body.Trim();
        if (string.IsNullOrWhiteSpace(title))
            return new GuestEmailTemplateSaveResult(false, "Title is required.", null);
        if (string.IsNullOrWhiteSpace(body))
            return new GuestEmailTemplateSaveResult(false, "Message body is required.", null);

        var category = GuestEmailTemplateCategories.All.Contains(model.Category)
            ? model.Category
            : GuestEmailTemplateCategories.General;

        var slug = string.IsNullOrWhiteSpace(model.PropertySlug)
            ? ""
            : model.PropertySlug.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(slug) &&
            await propertyService.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false) is null)
        {
            return new GuestEmailTemplateSaveResult(false, "Unknown property.", null);
        }

        GuestEmailTemplate entity;
        if (model.Id > 0)
        {
            entity = await db.GuestEmailTemplates
                .FirstOrDefaultAsync(t => t.Id == model.Id, cancellationToken)
                .ConfigureAwait(false)
                ?? new GuestEmailTemplate();

            if (entity.Id == 0)
                return new GuestEmailTemplateSaveResult(false, "Template not found.", null);
        }
        else
        {
            entity = new GuestEmailTemplate();
            db.GuestEmailTemplates.Add(entity);
        }

        entity.PropertySlug = slug;
        entity.Category = category;
        entity.Title = title;
        entity.EmailSubject = string.IsNullOrWhiteSpace(model.EmailSubject) ? null : model.EmailSubject.Trim();
        entity.Body = body;
        entity.SortOrder = model.SortOrder;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new GuestEmailTemplateSaveResult(true, null, entity.Id);
    }

    public async Task<GuestEmailTemplateSaveResult> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var entity = await db.GuestEmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return new GuestEmailTemplateSaveResult(false, "Template not found.", null);

        db.GuestEmailTemplates.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new GuestEmailTemplateSaveResult(true, null, null);
    }

    public string Render(string template, GuestEmailRenderContext context)
    {
        if (string.IsNullOrEmpty(template))
            return "";

        var firstName = context.GuestFullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            ?? context.GuestFullName;

        return template
            .Replace("{{GuestName}}", firstName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{GuestFullName}}", context.GuestFullName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{GuestEmail}}", context.GuestEmail, StringComparison.OrdinalIgnoreCase)
            .Replace("{{BookingNumber}}", context.BookingNumber, StringComparison.OrdinalIgnoreCase)
            .Replace("{{PropertyName}}", context.PropertyDisplayName, StringComparison.OrdinalIgnoreCase)
            .Replace("{{CheckIn}}", context.CheckIn.ToString("MMMM d, yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{CheckOut}}", context.CheckOut.ToString("MMMM d, yyyy"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{NightCount}}", context.NightCount.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{{TotalAmount}}", context.TotalAmount.ToString("C2"), StringComparison.OrdinalIgnoreCase)
            .Replace("{{PayUrl}}", context.PayUrl ?? "(payment link not available yet)", StringComparison.OrdinalIgnoreCase);
    }

    private async Task SeedDefaultsForPropertyAsync(
        string slug,
        string displayName,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var templates = new[]
        {
            new GuestEmailTemplate
            {
                PropertySlug = slug,
                Category = GuestEmailTemplateCategories.Welcome,
                Title = "Welcome & trip summary",
                EmailSubject = "Your stay at {{PropertyName}} — {{BookingNumber}}",
                SortOrder = 10,
                Body =
                    "Hello {{GuestName}},\n\n" +
                    "We are looking forward to hosting you at {{PropertyName}}.\n\n" +
                    "Your reservation: {{BookingNumber}}\n" +
                    "Check-in: {{CheckIn}}\n" +
                    "Check-out: {{CheckOut}}\n" +
                    "Nights: {{NightCount}}\n\n" +
                    "We will send check-in details closer to your arrival. Reply to this email if you have any questions.\n\n" +
                    "— Nestled Nooks",
                UpdatedAtUtc = now,
            },
            new GuestEmailTemplate
            {
                PropertySlug = slug,
                Category = GuestEmailTemplateCategories.CheckIn,
                Title = "Check-in instructions",
                EmailSubject = "Check-in instructions — {{PropertyName}}",
                SortOrder = 20,
                Body =
                    "Hello {{GuestName}},\n\n" +
                    "Your check-in at {{PropertyName}} is on {{CheckIn}} (4:00 PM unless we agreed otherwise).\n\n" +
                    "Access:\n" +
                    "• Address: [ADD STREET ADDRESS]\n" +
                    "• Door / key code: [ADD KEY CODE]\n" +
                    "• Wi‑Fi: see the guest guide in the house (or ask us to resend)\n\n" +
                    "Parking: [ADD PARKING NOTES]\n\n" +
                    "If anything looks wrong when you arrive, call or text us right away.\n\n" +
                    "— Nestled Nooks",
                UpdatedAtUtc = now,
            },
            new GuestEmailTemplate
            {
                PropertySlug = slug,
                Category = GuestEmailTemplateCategories.CheckOut,
                Title = "Check-out instructions",
                EmailSubject = "Check-out reminder — {{PropertyName}}",
                SortOrder = 30,
                Body =
                    "Hello {{GuestName}},\n\n" +
                    "We hope you enjoyed {{PropertyName}}. Check-out is {{CheckOut}} by 10:00 AM unless we agreed otherwise.\n\n" +
                    "Before you leave:\n" +
                    "• Strip used beds and leave linens in a pile\n" +
                    "• Start the dishwasher if you used dishes\n" +
                    "• Take trash to [ADD LOCATION]\n" +
                    "• Lock all doors and windows\n\n" +
                    "Safe travels — we would love to host you again.\n\n" +
                    "— Nestled Nooks",
                UpdatedAtUtc = now,
            },
            new GuestEmailTemplate
            {
                PropertySlug = slug,
                Category = GuestEmailTemplateCategories.Payment,
                Title = "Payment link",
                EmailSubject = "Payment due — {{BookingNumber}}",
                SortOrder = 40,
                Body =
                    "Hello {{GuestName}},\n\n" +
                    "Your stay at {{PropertyName}} ({{CheckIn}} → {{CheckOut}}) is approved.\n\n" +
                    "Booking total: {{TotalAmount}}\n" +
                    "Pay securely here:\n{{PayUrl}}\n\n" +
                    "— Nestled Nooks",
                UpdatedAtUtc = now,
            },
        };

        db.GuestEmailTemplates.AddRange(templates);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
