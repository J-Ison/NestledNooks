using System.ComponentModel.DataAnnotations;
using NestledNooks.Models;

namespace NestledNooks.Services;

public static class BookingFormReadiness
{
    public static IReadOnlyList<string> GetIssues(
        BookingFormModel model,
        PropertyBookingOptions? property,
        PropertyLegalSnapshot? legal,
        bool skipLegalAcceptance,
        string? quoteError = null,
        bool quoteLoading = false)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(model.GuestFullName))
            issues.Add("Enter your full name.");
        else if (model.GuestFullName.Trim().Length < 2)
            issues.Add("Full name must be at least 2 characters.");

        if (string.IsNullOrWhiteSpace(model.GuestEmail))
            issues.Add("Enter your email address.");
        else if (!new EmailAddressAttribute().IsValid(model.GuestEmail.Trim()))
            issues.Add("Enter a valid email address.");

        if (model.CheckIn is null)
            issues.Add("Choose a check-in date on the calendar.");
        else if (model.CheckOut is null)
            issues.Add("Choose a check-out date on the calendar.");
        else if (model.CheckOut <= model.CheckIn)
            issues.Add("Check-out must be after check-in.");

        if (property is not null)
        {
            if (model.GuestCount < 1)
                issues.Add("Guest count must be at least 1.");
            else if (model.GuestCount > property.MaxGuests)
                issues.Add($"Guest count cannot exceed {property.MaxGuests}.");

            if (model.PetCount < 0)
                issues.Add("Pet count cannot be negative.");
            else if (model.PetCount > property.MaxPets)
                issues.Add($"Pet count cannot exceed {property.MaxPets}.");
        }

        if (!string.IsNullOrWhiteSpace(quoteError))
            issues.Add(quoteError);

        if (model.CheckIn is not null &&
            model.CheckOut is not null &&
            model.CheckOut > model.CheckIn &&
            quoteLoading)
        {
            issues.Add("Wait for the price estimate to finish calculating.");
        }

        if (!skipLegalAcceptance && legal?.RequireGuestLegalAcceptance == true)
        {
            if (!model.AgreedToRentalAgreement)
                issues.Add("Check the box to accept the Rental agreement.");

            if (!model.AgreedToHouseRules)
                issues.Add("Check the box to accept the House rules.");

            if (!model.AgreedToLiabilityAcknowledgment)
                issues.Add("Check the box to accept the Liability & risk acknowledgment.");
        }

        return issues;
    }
}
