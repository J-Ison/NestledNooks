namespace NestledNooks.Data;

/// <summary>Additional staff-only label/value pairs for a property (blanks allowed until filled in).</summary>
public class PropertyCustomField
{
    public int Id { get; set; }

    public string PropertySlug { get; set; } = "";

    public string? Label { get; set; }

    public string? Value { get; set; }

    public int SortOrder { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
