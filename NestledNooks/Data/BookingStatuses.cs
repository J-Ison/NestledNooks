namespace NestledNooks.Data;

public static class BookingStatuses
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Denied = "Denied";
    public const string Cancelled = "Cancelled";
    public const string Active = "Active";
    public const string Ended = "Ended";

    /// <summary>Statuses that hold dates on the site calendar (and export iCal holds).</summary>
    public static readonly HashSet<string> DateHolding = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Approved,
        Active
    };
}
