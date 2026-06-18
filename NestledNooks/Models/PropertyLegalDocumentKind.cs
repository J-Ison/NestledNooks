namespace NestledNooks.Models;

public enum PropertyLegalDocumentKind
{
    RentalAgreement,
    HouseRules,
    LiabilityAcknowledgment,
}

public static class PropertyLegalDocumentKindExtensions
{
    public static string ToRouteSegment(this PropertyLegalDocumentKind kind) => kind switch
    {
        PropertyLegalDocumentKind.RentalAgreement => "rental-agreement",
        PropertyLegalDocumentKind.HouseRules => "house-rules",
        PropertyLegalDocumentKind.LiabilityAcknowledgment => "liability",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static string ToDisplayTitle(this PropertyLegalDocumentKind kind) => kind switch
    {
        PropertyLegalDocumentKind.RentalAgreement => "Rental agreement",
        PropertyLegalDocumentKind.HouseRules => "House rules",
        PropertyLegalDocumentKind.LiabilityAcknowledgment => "Liability & risk acknowledgment",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static bool TryParseRouteSegment(string? segment, out PropertyLegalDocumentKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(segment))
            return false;

        kind = segment.Trim().ToLowerInvariant() switch
        {
            "rental-agreement" => PropertyLegalDocumentKind.RentalAgreement,
            "house-rules" => PropertyLegalDocumentKind.HouseRules,
            "liability" => PropertyLegalDocumentKind.LiabilityAcknowledgment,
            _ => default,
        };

        return segment.Trim().ToLowerInvariant() is "rental-agreement" or "house-rules" or "liability";
    }
}
