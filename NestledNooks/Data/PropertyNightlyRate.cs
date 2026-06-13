namespace NestledNooks.Data;

/// <summary>Synced nightly rate for a property (typically from PriceLabs).</summary>
public class PropertyNightlyRate
{
    public int Id { get; set; }

    public string PropertySlug { get; set; } = "";

    public DateOnly Date { get; set; }

    public decimal Rate { get; set; }

    public int? MinimumStay { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
