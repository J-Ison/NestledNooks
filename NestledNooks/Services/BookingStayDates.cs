namespace NestledNooks.Services;

/// <summary>Lodging stay intervals use half-open ranges: [checkIn, checkOut) — checkout day is not an occupied night.</summary>
public static class BookingStayDates
{
    public static bool RangesOverlap(DateOnly stayCheckIn, DateOnly stayCheckOut, DateOnly blockCheckIn, DateOnly blockCheckOut) =>
        stayCheckIn < blockCheckOut && stayCheckOut > blockCheckIn;

    public static bool ExternalBlockAffectsStay(
        DateOnly stayCheckIn,
        DateOnly stayCheckOut,
        DateOnly blockCheckIn,
        DateOnly blockCheckOut) =>
        RangesOverlap(stayCheckIn, stayCheckOut, blockCheckIn, blockCheckOut);

    /// <summary>
    /// Airbnb/Vrbo iCal often ends with one long unavailable event when the channel booking window closes.
    /// </summary>
    public static DateOnly? FindChannelClosureStart(
        IEnumerable<(DateOnly Start, DateOnly End)> externalRanges,
        DateOnly today,
        int minimumSpanDays = 14)
    {
        DateOnly? earliest = null;

        foreach (var (start, end) in externalRanges)
        {
            if (start <= today)
                continue;

            if (end.DayNumber - start.DayNumber < minimumSpanDays)
                continue;

            if (earliest is null || start < earliest)
                earliest = start;
        }

        return earliest;
    }
}
