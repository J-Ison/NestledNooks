namespace NestledNooks.Data;

/// <summary>Cached busy range imported from Airbnb/Vrbo iCal export URLs.</summary>
public sealed class ExternalCalendarEvent
{
    public int Id { get; set; }
    public string PropertySlug { get; set; } = "";
    public string Source { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Summary { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
