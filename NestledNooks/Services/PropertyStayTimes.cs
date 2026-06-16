using System.Globalization;

namespace NestledNooks.Services;

public static class PropertyStayTimes
{
    public const string DefaultCheckInTime = "16:00";
    public const string DefaultCheckOutTime = "10:00";

    public static string FormatDisplay(string? time24, string fallback24)
    {
        var raw = string.IsNullOrWhiteSpace(time24) ? fallback24 : time24.Trim();
        if (TimeOnly.TryParse(raw, CultureInfo.InvariantCulture, out var parsed))
            return parsed.ToString("h:mm tt", CultureInfo.InvariantCulture);

        return raw;
    }

    public static (string CheckIn, string CheckOut) Resolve(PropertyBookingOptions? property) =>
        (
            FormatDisplay(property?.CheckInTime, DefaultCheckInTime),
            FormatDisplay(property?.CheckOutTime, DefaultCheckOutTime));
}
