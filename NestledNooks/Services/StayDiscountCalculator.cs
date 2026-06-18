using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed record StayDiscountResult(decimal Percent, string Label)
{
    public static StayDiscountResult None { get; } = new(0m, "");

    public bool HasDiscount => Percent > 0 && !string.IsNullOrWhiteSpace(Label);
}

public static class StayDiscountCalculator
{
    public static StayDiscountResult CalculateBestDiscount(
        PropertyBookingDiscounts discounts,
        int nights,
        DateOnly checkIn,
        DateOnly bookedOn)
    {
        var leadDays = checkIn.DayNumber - bookedOn.DayNumber;
        var candidates = new List<StayDiscountResult>();

        if (discounts.MonthlyEnabled && nights >= discounts.MonthlyMinNights)
            candidates.Add(new StayDiscountResult(discounts.MonthlyPercent, "Monthly discount"));

        if (discounts.WeeklyEnabled && nights >= discounts.WeeklyMinNights)
            candidates.Add(new StayDiscountResult(discounts.WeeklyPercent, "Weekly discount"));

        if (discounts.LastMinuteEnabled && leadDays <= discounts.LastMinuteMaxDaysBeforeArrival)
            candidates.Add(new StayDiscountResult(discounts.LastMinutePercent, "Last-minute discount"));

        if (discounts.EarlyBirdEnabled && leadDays >= discounts.EarlyBirdMinDaysBeforeArrival)
            candidates.Add(new StayDiscountResult(discounts.EarlyBirdPercent, "Early-bird discount"));

        foreach (var promo in discounts.CustomPromotions.Where(p => p.Enabled && p.Percent > 0))
        {
            if (!IsCheckInInRange(checkIn, promo.StayCheckInFrom, promo.StayCheckInTo))
                continue;

            var label = string.IsNullOrWhiteSpace(promo.Name) ? "Custom promotion" : promo.Name.Trim();
            candidates.Add(new StayDiscountResult(promo.Percent, label));
        }

        // Only one discount applies per stay — the single highest eligible percent wins.
        return candidates.Count == 0
            ? StayDiscountResult.None
            : candidates.MaxBy(c => c.Percent)!;
    }

    public static decimal CalculateDiscountAmount(decimal lodgingSubtotal, StayDiscountResult discount) =>
        discount.HasDiscount
            ? Math.Round(lodgingSubtotal * discount.Percent / 100m, 2, MidpointRounding.AwayFromZero)
            : 0m;

    private static bool IsCheckInInRange(DateOnly checkIn, DateOnly? from, DateOnly? to)
    {
        if (from is { } start && checkIn < start)
            return false;

        if (to is { } end && checkIn > end)
            return false;

        return from is not null || to is not null;
    }
}
