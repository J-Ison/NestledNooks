using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using NestledNooks.Data;

using NestledNooks.Models;



namespace NestledNooks.Services;



public sealed class BookingPricingService

{

    private readonly ApplicationDbContext _db;

    private readonly BookingOptions _options;



    public BookingPricingService(

        ApplicationDbContext db,

        IOptions<BookingOptions> options)

    {

        _db = db;

        _options = options.Value;

    }



    public IReadOnlyList<PropertyBookingOptions> GetAllProperties() => _options.Properties;



    public PropertyBookingOptions? GetProperty(string slug) =>

        _options.Properties.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));



    public async Task<BookingQuote> CalculateAsync(

        string propertySlug,

        DateOnly checkIn,

        DateOnly checkOut,

        int petCount,

        CancellationToken cancellationToken = default)

    {

        var property = GetProperty(propertySlug)

            ?? throw new InvalidOperationException("Unknown property.");



        var nights = checkOut.DayNumber - checkIn.DayNumber;

        if (nights < 1)

            throw new InvalidOperationException("Stay must be at least one night.");



        var listing = await GetListingSettingsAsync(propertySlug, cancellationToken).ConfigureAwait(false);

        ValidateBookingWindow(checkIn, listing);



        var slug = property.Slug.Trim().ToLowerInvariant();

        var syncedRates = await _db.PropertyNightlyRates

            .AsNoTracking()

            .Where(r => r.PropertySlug == slug && r.Date >= checkIn && r.Date < checkOut)

            .ToListAsync(cancellationToken)

            .ConfigureAwait(false);



        var rateByDate = syncedRates.ToDictionary(r => r.Date, r => r);

        var usesDynamicPricing = syncedRates.Count > 0;



        var requiredMinimumNights = listing.MinimumNights;



        if (nights < requiredMinimumNights)

            throw new InvalidOperationException($"Minimum stay is {requiredMinimumNights} nights.");



        decimal subtotal = 0;

        for (var date = checkIn; date < checkOut; date = date.AddDays(1))

        {

            var nightlyRate = rateByDate.TryGetValue(date, out var synced)

                ? synced.Rate

                : property.NightlyRate;



            subtotal += nightlyRate;

        }



        var averageNightly = Math.Round(subtotal / nights, 2, MidpointRounding.AwayFromZero);

        var petFee = ListingSettingsDefaults.CalculatePetDeposit(petCount, listing.PetDepositPerTwoPets);

        var cleaningFee = listing.CleaningFee;

        var total = subtotal + cleaningFee + petFee;



        return new BookingQuote(

            nights,

            averageNightly,

            subtotal,

            cleaningFee,

            petFee,

            total,

            usesDynamicPricing);

    }



    public async Task<PropertyListingSettings> GetListingSettingsAsync(

        string propertySlug,

        CancellationToken cancellationToken = default)

    {

        if (string.IsNullOrWhiteSpace(propertySlug))

            return PropertyListingSettings.Defaults();



        var slug = propertySlug.Trim().ToLowerInvariant();

        var row = await _db.RentalProperties

            .AsNoTracking()

            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken)

            .ConfigureAwait(false);



        return PropertyListingSettings.FromEntity(row);

    }



    public static void ValidateBookingWindow(DateOnly checkIn, PropertyListingSettings listing)

    {

        var earliest = listing.EarliestCheckInUtc;

        var latest = listing.LatestCheckInUtc;



        if (checkIn < earliest)

        {

            throw new InvalidOperationException(

                $"Check-in must be at least {listing.MinAdvanceBookingDays} days from today.");

        }



        if (checkIn > latest)
        {
            throw new InvalidOperationException(
                $"Check-in cannot be more than {listing.EffectiveMaxBookingDaysAhead} days from today.");
        }

    }

}

