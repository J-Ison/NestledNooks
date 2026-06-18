using System.Text.Json.Serialization;

namespace NestledNooks.Models;

public sealed class PropertyBookingDiscounts
{
    public bool WeeklyEnabled { get; set; } = true;

    public decimal WeeklyPercent { get; set; } = 10m;

    public int WeeklyMinNights { get; set; } = 7;

    public bool MonthlyEnabled { get; set; } = true;

    public decimal MonthlyPercent { get; set; } = 27m;

    public int MonthlyMinNights { get; set; } = 28;

    public bool LastMinuteEnabled { get; set; } = true;

    public decimal LastMinutePercent { get; set; } = 15m;

    public int LastMinuteMaxDaysBeforeArrival { get; set; } = 14;

    public bool EarlyBirdEnabled { get; set; } = true;

    public decimal EarlyBirdPercent { get; set; } = 5m;

    public int EarlyBirdMinDaysBeforeArrival { get; set; } = 90;

    public List<PropertyCustomPromotion> CustomPromotions { get; set; } = [];

    public static PropertyBookingDiscounts Defaults() => new();
}

public sealed class PropertyCustomPromotion
{
    public string Name { get; set; } = "";

    public decimal Percent { get; set; }

    public DateOnly? StayCheckInFrom { get; set; }

    public DateOnly? StayCheckInTo { get; set; }

    public bool Enabled { get; set; } = true;

    [JsonIgnore]
    public DateTime? StayCheckInFromDate
    {
        get => StayCheckInFrom?.ToDateTime(TimeOnly.MinValue);
        set => StayCheckInFrom = value is null ? null : DateOnly.FromDateTime(value.Value.Date);
    }

    [JsonIgnore]
    public DateTime? StayCheckInToDate
    {
        get => StayCheckInTo?.ToDateTime(TimeOnly.MinValue);
        set => StayCheckInTo = value is null ? null : DateOnly.FromDateTime(value.Value.Date);
    }
}
