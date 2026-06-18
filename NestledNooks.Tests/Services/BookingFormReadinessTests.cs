using NestledNooks.Models;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class BookingFormReadinessTests
{
    [Fact]
    public void GetIssues_ListsEachMissingRequirement()
    {
        var model = new BookingFormModel
        {
            GuestFullName = "",
            GuestEmail = "not-an-email",
            CheckIn = null,
            CheckOut = null,
        };

        var legal = new PropertyLegalSnapshot
        {
            RequireGuestLegalAcceptance = true,
        };

        var issues = BookingFormReadiness.GetIssues(
            model,
            property: new PropertyBookingOptions { MaxGuests = 12, MaxPets = 4 },
            legal,
            skipLegalAcceptance: false);

        Assert.Contains(issues, i => i.Contains("full name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, i => i.Contains("email", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, i => i.Contains("check-in", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, i => i.Contains("Rental agreement", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, i => i.Contains("House rules", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(issues, i => i.Contains("Liability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetIssues_ReturnsEmptyWhenReady()
    {
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(20);
        var model = new BookingFormModel
        {
            GuestFullName = "Jordan Guest",
            GuestEmail = "guest@example.com",
            CheckIn = checkIn,
            CheckOut = checkIn.AddDays(3),
            GuestCount = 2,
            AgreedToRentalAgreement = true,
            AgreedToHouseRules = true,
            AgreedToLiabilityAcknowledgment = true,
        };

        var issues = BookingFormReadiness.GetIssues(
            model,
            new PropertyBookingOptions { MaxGuests = 12, MaxPets = 4 },
            new PropertyLegalSnapshot { RequireGuestLegalAcceptance = true },
            skipLegalAcceptance: false);

        Assert.Empty(issues);
    }
}
