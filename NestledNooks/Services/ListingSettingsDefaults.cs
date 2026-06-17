namespace NestledNooks.Services;

/// <summary>Default direct-booking listing rules when SiteSettings row is missing values.</summary>
public static class ListingSettingsDefaults
{
    public const int MinimumNights = 2;
    public const int MinAdvanceBookingDays = 10;
    public const int MaxBookingDaysAhead = 365;
    public const decimal CleaningFee = 200m;
    public const decimal PetDepositPerTwoPets = 50m;

    /// <summary>Only honor Airbnb/Vrbo iCal blocks within this many days (Airbnb export is ~6 months).</summary>
    public const int ExternalCalendarTrustDays = 180;

    public const bool AllowFarAdvanceDirectBooking = true;

    public static int ClampMinimumNights(int value) => Math.Clamp(value, 1, 30);

    public static int ClampMinAdvanceBookingDays(int value) => Math.Clamp(value, 0, 90);

    public static int ClampMaxBookingDaysAhead(int value) => Math.Clamp(value, 30, 730);

    public static decimal ClampCleaningFee(decimal value) => Math.Clamp(value, 0m, 10_000m);

    public static decimal ClampPetDepositPerTwoPets(decimal value) => Math.Clamp(value, 0m, 5_000m);

    public static int ClampExternalCalendarTrustDays(int value) => Math.Clamp(value, 0, 730);

    public static decimal CalculatePetDeposit(int petCount, decimal depositPerTwoPets)
    {
        if (petCount <= 0 || depositPerTwoPets <= 0)
            return 0m;

        var pairs = (int)Math.Ceiling(petCount / 2.0);
        return pairs * depositPerTwoPets;
    }
}
