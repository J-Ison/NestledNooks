using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class PropertyListingSettingsTests
{
    [Fact]
    public void EffectiveMaxBookingDaysAhead_CapsAtTrustWindowWhenFarAdvanceDisabled()
    {
        var listing = new PropertyListingSettings(
            MinimumNights: 2,
            MinAdvanceBookingDays: 10,
            MaxBookingDaysAhead: 365,
            CleaningFee: 200,
            PetDepositPerTwoPets: 50,
            ExternalCalendarTrustDays: 180,
            AllowFarAdvanceDirectBooking: false);

        Assert.Equal(180, listing.EffectiveMaxBookingDaysAhead);
    }

    [Fact]
    public void EffectiveMaxBookingDaysAhead_UsesBookUpToWhenFarAdvanceEnabled()
    {
        var listing = new PropertyListingSettings(
            MinimumNights: 2,
            MinAdvanceBookingDays: 10,
            MaxBookingDaysAhead: 365,
            CleaningFee: 200,
            PetDepositPerTwoPets: 50,
            ExternalCalendarTrustDays: 180,
            AllowFarAdvanceDirectBooking: true);

        Assert.Equal(365, listing.EffectiveMaxBookingDaysAhead);
    }

    [Fact]
    public void ValidateBookingWindow_RejectsBeyondTrustWhenFarAdvanceDisabled()
    {
        var listing = new PropertyListingSettings(
            MinimumNights: 2,
            MinAdvanceBookingDays: 0,
            MaxBookingDaysAhead: 365,
            CleaningFee: 200,
            PetDepositPerTwoPets: 50,
            ExternalCalendarTrustDays: 180,
            AllowFarAdvanceDirectBooking: false);

        var tooFar = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(200);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            BookingPricingService.ValidateBookingWindow(tooFar, listing));

        Assert.Contains("180", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IsBeyondExternalCalendarTrust_OnlyWhenFarAdvanceEnabled()
    {
        var listing = new PropertyListingSettings(
            MinimumNights: 2,
            MinAdvanceBookingDays: 10,
            MaxBookingDaysAhead: 365,
            CleaningFee: 200,
            PetDepositPerTwoPets: 50,
            ExternalCalendarTrustDays: 180,
            AllowFarAdvanceDirectBooking: false);

        var farCheckIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(200);
        Assert.False(listing.IsBeyondExternalCalendarTrust(farCheckIn));
    }
}
