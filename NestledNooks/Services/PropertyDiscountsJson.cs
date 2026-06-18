using System.Text.Json;
using NestledNooks.Models;

namespace NestledNooks.Services;

public static class PropertyDiscountsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static PropertyBookingDiscounts Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return PropertyBookingDiscounts.Defaults();

        try
        {
            return JsonSerializer.Deserialize<PropertyBookingDiscounts>(json, Options)
                ?? PropertyBookingDiscounts.Defaults();
        }
        catch (JsonException)
        {
            return PropertyBookingDiscounts.Defaults();
        }
    }

    public static string Serialize(PropertyBookingDiscounts discounts) =>
        JsonSerializer.Serialize(Normalize(discounts), Options);

    private static PropertyBookingDiscounts Normalize(PropertyBookingDiscounts discounts)
    {
        discounts.WeeklyPercent = ClampPercent(discounts.WeeklyPercent);
        discounts.MonthlyPercent = ClampPercent(discounts.MonthlyPercent);
        discounts.LastMinutePercent = ClampPercent(discounts.LastMinutePercent);
        discounts.EarlyBirdPercent = ClampPercent(discounts.EarlyBirdPercent);
        discounts.WeeklyMinNights = Math.Clamp(discounts.WeeklyMinNights, 1, 365);
        discounts.MonthlyMinNights = Math.Clamp(discounts.MonthlyMinNights, 1, 365);
        discounts.LastMinuteMaxDaysBeforeArrival = Math.Clamp(discounts.LastMinuteMaxDaysBeforeArrival, 0, 365);
        discounts.EarlyBirdMinDaysBeforeArrival = Math.Clamp(discounts.EarlyBirdMinDaysBeforeArrival, 0, 730);

        discounts.CustomPromotions = discounts.CustomPromotions
            .Where(p => p.Enabled || p.Percent > 0 || !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new PropertyCustomPromotion
            {
                Name = p.Name.Trim(),
                Percent = ClampPercent(p.Percent),
                StayCheckInFrom = p.StayCheckInFrom,
                StayCheckInTo = p.StayCheckInTo,
                Enabled = p.Enabled,
            })
            .Where(p => p.Enabled && p.Percent > 0)
            .ToList();

        return discounts;
    }

    private static decimal ClampPercent(decimal value) => Math.Clamp(value, 0m, 100m);
}
