using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Models;

public sealed class PropertyLegalSnapshot
{
    public string PropertySlug { get; init; } = "";

    public string PropertyDisplayName { get; init; } = "";

    public int LegalDocumentsVersion { get; init; } = 1;

    public bool RequireGuestLegalAcceptance { get; init; } = true;

    public string RentalAgreementText { get; init; } = "";

    public string HouseRulesText { get; init; } = "";

    public string LiabilityAcknowledgmentText { get; init; } = "";

    public static PropertyLegalSnapshot FromEntity(RentalProperty? property)
    {
        if (property is null)
            return new PropertyLegalSnapshot();

        var name = string.IsNullOrWhiteSpace(property.DisplayName) ? property.Slug : property.DisplayName;
        return new PropertyLegalSnapshot
        {
            PropertySlug = property.Slug,
            PropertyDisplayName = name,
            LegalDocumentsVersion = property.LegalDocumentsVersion,
            RequireGuestLegalAcceptance = property.RequireGuestLegalAcceptance,
            RentalAgreementText = ResolveText(property.RentalAgreementText, PropertyLegalDefaults.RentalAgreement(name)),
            HouseRulesText = ResolveText(property.HouseRulesText, PropertyLegalDefaults.HouseRules(name)),
            LiabilityAcknowledgmentText = ResolveText(
                property.LiabilityAcknowledgmentText,
                PropertyLegalDefaults.LiabilityAcknowledgment(name)),
        };
    }

    public string GetText(PropertyLegalDocumentKind kind) => kind switch
    {
        PropertyLegalDocumentKind.RentalAgreement => RentalAgreementText,
        PropertyLegalDocumentKind.HouseRules => HouseRulesText,
        PropertyLegalDocumentKind.LiabilityAcknowledgment => LiabilityAcknowledgmentText,
        _ => "",
    };

    private static string ResolveText(string? stored, string fallback) =>
        string.IsNullOrWhiteSpace(stored) ? fallback : stored.Trim();
}
