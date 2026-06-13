namespace NestledNooks.Data;

/// <summary>Vendor or emergency contact for a rental property (staff-only).</summary>
public class PropertyContact
{
    public int Id { get; set; }

    public string PropertySlug { get; set; } = "";

    /// <summary>Person or company name.</summary>
    public string Name { get; set; } = "";

    /// <summary>e.g. Plumber, Handyman, HVAC.</summary>
    public string? Role { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Notes { get; set; }

    public int SortOrder { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
