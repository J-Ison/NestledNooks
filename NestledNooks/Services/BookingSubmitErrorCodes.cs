namespace NestledNooks.Services;

/// <summary>Reference codes shown to guests when a booking request fails (for host support).</summary>
public static class BookingSubmitErrorCodes
{
    public const string DirectBookingDisabled = "BK-001";
    public const string UnknownProperty = "BK-002";
    public const string MissingDates = "BK-003";
    public const string TooManyGuests = "BK-004";
    public const string TooManyPets = "BK-005";
    public const string QuoteFailed = "BK-006";
    public const string DatesUnavailable = "BK-007";
    public const string SaveFailed = "BK-008";
    public const string Unexpected = "BK-009";
    public const string OutsideBookingWindow = "BK-010";
    public const string LegalAcceptanceRequired = "BK-011";

    public static string FormatGuestMessage(string code, string message) =>
        $"{message} (Ref: {code})";

    public static string? DescribeForStaff(string? code) => code switch
    {
        DirectBookingDisabled => "Direct booking toggle is off and submitter is not Owner.",
        UnknownProperty => "Property slug in form does not match Booking:Properties config.",
        MissingDates => "Check-in or check-out missing when form was submitted.",
        TooManyGuests => "Guest count exceeds property MaxGuests.",
        TooManyPets => "Pet count exceeds property MaxPets.",
        QuoteFailed => "Pricing failed (minimum stay, missing rates table, or bad date range).",
        DatesUnavailable => "Selected range overlaps a hold or external calendar block.",
        SaveFailed => "SQL/EF error while saving BookingRequests row.",
        OutsideBookingWindow => "Check-in outside allowed booking window (advance notice or max days ahead).",
        LegalAcceptanceRequired => "Guest did not accept required rental agreements.",
        Unexpected => "Unhandled exception during submit.",
        _ => null,
    };
}
