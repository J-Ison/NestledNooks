using NestledNooks.Data;

namespace NestledNooks.Services;

/// <summary>Per-property direct booking rules and fees.</summary>
public sealed record PropertyListingSettings(
    int MinimumNights,
    int MinAdvanceBookingDays,
    int MaxBookingDaysAhead,
    decimal CleaningFee,
    decimal PetDepositPerTwoPets,
    int ExternalCalendarTrustDays,
    bool AllowFarAdvanceDirectBooking)
{
    public static PropertyListingSettings Defaults() =>
        new(
            ListingSettingsDefaults.MinimumNights,
            ListingSettingsDefaults.MinAdvanceBookingDays,
            ListingSettingsDefaults.MaxBookingDaysAhead,
            ListingSettingsDefaults.CleaningFee,
            ListingSettingsDefaults.PetDepositPerTwoPets,
            ListingSettingsDefaults.ExternalCalendarTrustDays,
            ListingSettingsDefaults.AllowFarAdvanceDirectBooking);

    public static PropertyListingSettings FromEntity(RentalProperty? property) =>
        property is null
            ? Defaults()
            : new PropertyListingSettings(
                ListingSettingsDefaults.ClampMinimumNights(property.MinimumNights),
                ListingSettingsDefaults.ClampMinAdvanceBookingDays(property.MinAdvanceBookingDays),
                ListingSettingsDefaults.ClampMaxBookingDaysAhead(property.MaxBookingDaysAhead),
                ListingSettingsDefaults.ClampCleaningFee(property.CleaningFee),
                ListingSettingsDefaults.ClampPetDepositPerTwoPets(property.PetDepositPerTwoPets),
                ListingSettingsDefaults.ClampExternalCalendarTrustDays(property.ExternalCalendarTrustDays),
                property.AllowFarAdvanceDirectBooking);

    public DateOnly EarliestCheckInUtc =>
        DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(MinAdvanceBookingDays);

    public DateOnly LatestCheckInUtc =>
        DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(EffectiveMaxBookingDaysAhead);

    public int EffectiveMaxBookingDaysAhead =>
        !AllowFarAdvanceDirectBooking && ExternalCalendarTrustDays > 0
            ? Math.Min(MaxBookingDaysAhead, ExternalCalendarTrustDays)
            : MaxBookingDaysAhead;

    public DateOnly ExternalCalendarTrustThrough =>
        DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(ExternalCalendarTrustDays);

    public bool IsBeyondExternalCalendarTrust(DateOnly checkIn) =>
        AllowFarAdvanceDirectBooking
        && ExternalCalendarTrustDays > 0
        && checkIn > ExternalCalendarTrustThrough;
}
