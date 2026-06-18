using NestledNooks.Models;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class StayDiscountCalculatorTests
{
    private static readonly PropertyBookingDiscounts AirbnbDefaults = PropertyBookingDiscounts.Defaults();

    [Fact]
    public void CalculateBestDiscount_UsesWeeklyForSevenNights()
    {
        var result = StayDiscountCalculator.CalculateBestDiscount(
            AirbnbDefaults,
            nights: 7,
            checkIn: new DateOnly(2026, 8, 1),
            bookedOn: new DateOnly(2026, 6, 1));

        Assert.Equal(10m, result.Percent);
        Assert.Equal("Weekly discount", result.Label);
    }

    [Fact]
    public void CalculateBestDiscount_UsesMonthlyForTwentyEightNights()
    {
        var result = StayDiscountCalculator.CalculateBestDiscount(
            AirbnbDefaults,
            nights: 28,
            checkIn: new DateOnly(2026, 9, 1),
            bookedOn: new DateOnly(2026, 6, 1));

        Assert.Equal(27m, result.Percent);
        Assert.Equal("Monthly discount", result.Label);
    }

    [Fact]
    public void CalculateBestDiscount_PrefersLastMinuteOverWeekly()
    {
        var result = StayDiscountCalculator.CalculateBestDiscount(
            AirbnbDefaults,
            nights: 7,
            checkIn: new DateOnly(2026, 6, 20),
            bookedOn: new DateOnly(2026, 6, 10));

        Assert.Equal(15m, result.Percent);
        Assert.Equal("Last-minute discount", result.Label);
    }

    [Fact]
    public void CalculateBestDiscount_AppliesOnlyOneDiscount_WhenMultipleAreEligible()
    {
        var result = StayDiscountCalculator.CalculateBestDiscount(
            AirbnbDefaults,
            nights: 28,
            checkIn: new DateOnly(2026, 6, 20),
            bookedOn: new DateOnly(2026, 6, 10));

        // 28 nights qualifies for weekly (10%), monthly (27%), and last-minute (15%).
        Assert.Equal(27m, result.Percent);
        Assert.Equal("Monthly discount", result.Label);
    }

    [Fact]
    public void CalculateDiscountAmount_AppliesSinglePercentOnly()
    {
        var discount = new StayDiscountResult(27m, "Monthly discount");
        var amount = StayDiscountCalculator.CalculateDiscountAmount(1000m, discount);

        Assert.Equal(270m, amount);
    }

    [Fact]
    public void CalculateBestDiscount_UsesCustomPromotionWhenHighest()
    {
        var discounts = PropertyBookingDiscounts.Defaults();
        discounts.CustomPromotions.Add(new PropertyCustomPromotion
        {
            Name = "Summer special",
            Percent = 21m,
            StayCheckInFrom = new DateOnly(2026, 6, 22),
            StayCheckInTo = new DateOnly(2026, 6, 23),
            Enabled = true,
        });

        var result = StayDiscountCalculator.CalculateBestDiscount(
            discounts,
            nights: 2,
            checkIn: new DateOnly(2026, 6, 22),
            bookedOn: new DateOnly(2026, 6, 1));

        Assert.Equal(21m, result.Percent);
        Assert.Equal("Summer special", result.Label);
    }
}
