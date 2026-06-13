using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class PropertyOperationsPageData
{
    public required string PropertySlug { get; init; }
    public required string PropertyDisplayName { get; init; }
    public required IReadOnlyList<PropertyEquipmentItemModel> EquipmentItems { get; init; }
    public required IReadOnlyList<PropertyContactModel> Contacts { get; init; }
    public required IReadOnlyList<PropertyCustomFieldModel> CustomFields { get; init; }
}

public sealed class PropertyEquipmentItemModel
{
    public int Id { get; set; }
    public string? Item { get; set; }
    public string? Value { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PropertyContactModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PropertyCustomFieldModel
{
    public int Id { get; set; }
    public string? Label { get; set; }
    public string? Value { get; set; }
    public int SortOrder { get; set; }
}

public sealed class PropertyOperationsSaveResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? Id { get; init; }

    public static PropertyOperationsSaveResult Ok(int? id = null) =>
        new() { Success = true, Id = id };

    public static PropertyOperationsSaveResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}

public interface IPropertyOperationsService
{
    Task<PropertyOperationsPageData?> GetPageDataAsync(string propertySlug, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> SaveEquipmentItemAsync(string propertySlug, PropertyEquipmentItemModel item, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> DeleteEquipmentItemAsync(string propertySlug, int itemId, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> SaveContactAsync(string propertySlug, PropertyContactModel contact, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> DeleteContactAsync(string propertySlug, int contactId, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> SaveCustomFieldAsync(string propertySlug, PropertyCustomFieldModel field, CancellationToken cancellationToken = default);
    Task<PropertyOperationsSaveResult> DeleteCustomFieldAsync(string propertySlug, int fieldId, CancellationToken cancellationToken = default);
}
