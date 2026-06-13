namespace NestledNooks.Data;

/// <summary>Staff-only equipment and access details for a property (item + value pairs).</summary>
public class PropertyEquipmentItem
{
    public int Id { get; set; }

    public string PropertySlug { get; set; } = "";

    /// <summary>e.g. Lockbox, Doorbell Camera, Door Smartlock</summary>
    public string? Item { get; set; }

    /// <summary>e.g. code, URL, model name — blanks allowed until filled in.</summary>
    public string? Value { get; set; }

    public int SortOrder { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
