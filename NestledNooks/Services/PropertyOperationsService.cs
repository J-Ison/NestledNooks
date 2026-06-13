using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class PropertyOperationsService(
    ApplicationDbContext db,
    IPropertyService propertyService,
    IHttpContextAccessor httpContextAccessor) : IPropertyOperationsService
{
    public async Task<PropertyOperationsPageData?> GetPageDataAsync(
        string propertySlug,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return null;

        var property = await propertyService.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        if (property is null)
            return null;

        var equipmentItems = await db.PropertyEquipmentItems
            .AsNoTracking()
            .Where(e => e.PropertySlug == slug)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.Id)
            .Select(e => new PropertyEquipmentItemModel
            {
                Id = e.Id,
                Item = e.Item,
                Value = e.Value,
                SortOrder = e.SortOrder,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var contacts = await db.PropertyContacts
            .AsNoTracking()
            .Where(c => c.PropertySlug == slug)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new PropertyContactModel
            {
                Id = c.Id,
                Name = c.Name,
                Role = c.Role,
                Phone = c.Phone,
                Email = c.Email,
                Notes = c.Notes,
                SortOrder = c.SortOrder,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var customFields = await db.PropertyCustomFields
            .AsNoTracking()
            .Where(f => f.PropertySlug == slug)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Id)
            .Select(f => new PropertyCustomFieldModel
            {
                Id = f.Id,
                Label = f.Label,
                Value = f.Value,
                SortOrder = f.SortOrder,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PropertyOperationsPageData
        {
            PropertySlug = slug,
            PropertyDisplayName = property.DisplayName,
            EquipmentItems = equipmentItems,
            Contacts = contacts,
            CustomFields = customFields,
        };
    }

    public async Task<PropertyOperationsSaveResult> SaveEquipmentItemAsync(
        string propertySlug,
        PropertyEquipmentItemModel item,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        if (!await PropertyExistsAsync(slug, cancellationToken).ConfigureAwait(false))
            return PropertyOperationsSaveResult.Fail("Property not found.");

        PropertyEquipmentItem entity;
        if (item.Id > 0)
        {
            var existing = await db.PropertyEquipmentItems
                .FirstOrDefaultAsync(e => e.Id == item.Id && e.PropertySlug == slug, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
                return PropertyOperationsSaveResult.Fail("Equipment item not found.");
            entity = existing;
        }
        else
        {
            entity = new PropertyEquipmentItem { PropertySlug = slug };
            var maxSort = await db.PropertyEquipmentItems
                .Where(e => e.PropertySlug == slug)
                .Select(e => (int?)e.SortOrder)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false);
            entity.SortOrder = (maxSort ?? -1) + 1;
            db.PropertyEquipmentItems.Add(entity);
        }

        entity.Item = TrimOptional(item.Item, 200);
        entity.Value = TrimOptional(item.Value, 1000);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok(entity.Id);
    }

    public async Task<PropertyOperationsSaveResult> DeleteEquipmentItemAsync(
        string propertySlug,
        int itemId,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        var entity = await db.PropertyEquipmentItems
            .FirstOrDefaultAsync(e => e.Id == itemId && e.PropertySlug == slug, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return PropertyOperationsSaveResult.Fail("Equipment item not found.");

        db.PropertyEquipmentItems.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok();
    }

    public async Task<PropertyOperationsSaveResult> SaveContactAsync(
        string propertySlug,
        PropertyContactModel contact,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        if (!await PropertyExistsAsync(slug, cancellationToken).ConfigureAwait(false))
            return PropertyOperationsSaveResult.Fail("Property not found.");

        var name = contact.Name?.Trim() ?? "";
        if (name.Length < 1)
            return PropertyOperationsSaveResult.Fail("Contact name is required.");

        PropertyContact entity;
        if (contact.Id > 0)
        {
            var existing = await db.PropertyContacts
                .FirstOrDefaultAsync(c => c.Id == contact.Id && c.PropertySlug == slug, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
                return PropertyOperationsSaveResult.Fail("Contact not found.");
            entity = existing;
        }
        else
        {
            entity = new PropertyContact { PropertySlug = slug };
            var maxSort = await db.PropertyContacts
                .Where(c => c.PropertySlug == slug)
                .Select(c => (int?)c.SortOrder)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false);
            entity.SortOrder = (maxSort ?? -1) + 1;
            db.PropertyContacts.Add(entity);
        }

        entity.Name = name.Length > 200 ? name[..200] : name;
        entity.Role = TrimOptional(contact.Role, 120);
        entity.Phone = TrimOptional(contact.Phone, 60);
        entity.Email = TrimOptional(contact.Email, 256);
        entity.Notes = TrimOptional(contact.Notes, 2000);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok(entity.Id);
    }

    public async Task<PropertyOperationsSaveResult> DeleteContactAsync(
        string propertySlug,
        int contactId,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        var entity = await db.PropertyContacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.PropertySlug == slug, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return PropertyOperationsSaveResult.Fail("Contact not found.");

        db.PropertyContacts.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok();
    }

    public async Task<PropertyOperationsSaveResult> SaveCustomFieldAsync(
        string propertySlug,
        PropertyCustomFieldModel field,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        if (!await PropertyExistsAsync(slug, cancellationToken).ConfigureAwait(false))
            return PropertyOperationsSaveResult.Fail("Property not found.");

        PropertyCustomField entity;
        if (field.Id > 0)
        {
            var existing = await db.PropertyCustomFields
                .FirstOrDefaultAsync(f => f.Id == field.Id && f.PropertySlug == slug, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
                return PropertyOperationsSaveResult.Fail("Field not found.");
            entity = existing;
        }
        else
        {
            entity = new PropertyCustomField { PropertySlug = slug };
            var maxSort = await db.PropertyCustomFields
                .Where(f => f.PropertySlug == slug)
                .Select(f => (int?)f.SortOrder)
                .MaxAsync(cancellationToken)
                .ConfigureAwait(false);
            entity.SortOrder = (maxSort ?? -1) + 1;
            db.PropertyCustomFields.Add(entity);
        }

        entity.Label = TrimOptional(field.Label, 200);
        entity.Value = TrimOptional(field.Value, 1000);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok(entity.Id);
    }

    public async Task<PropertyOperationsSaveResult> DeleteCustomFieldAsync(
        string propertySlug,
        int fieldId,
        CancellationToken cancellationToken = default)
    {
        HostStaffAuthorization.EnsureHostStaff(httpContextAccessor.HttpContext?.User);

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return PropertyOperationsSaveResult.Fail("Invalid property.");

        var entity = await db.PropertyCustomFields
            .FirstOrDefaultAsync(f => f.Id == fieldId && f.PropertySlug == slug, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
            return PropertyOperationsSaveResult.Fail("Field not found.");

        db.PropertyCustomFields.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return PropertyOperationsSaveResult.Ok();
    }

    private async Task<bool> PropertyExistsAsync(string slug, CancellationToken cancellationToken) =>
        await propertyService.GetBySlugAsync(slug, cancellationToken).ConfigureAwait(false) is not null;

    private static string? NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var normalized = slug.Trim().ToLowerInvariant();
        return normalized.Length > 120 ? normalized[..120] : normalized;
    }

    private static string? TrimOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
