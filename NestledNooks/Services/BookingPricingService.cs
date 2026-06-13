using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed class BookingPricingService
{
    private readonly ApplicationDbContext _db;
    private readonly BookingOptions _options;

    public BookingPricingService(ApplicationDbContext db, IOptions<BookingOptions> options)
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

        var slug = property.Slug.Trim().ToLowerInvariant();
        var syncedRates = await _db.PropertyNightlyRates
            .AsNoTracking()
            .Where(r => r.PropertySlug == slug && r.Date >= checkIn && r.Date < checkOut)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rateByDate = syncedRates.ToDictionary(r => r.Date, r => r);
        var usesDynamicPricing = syncedRates.Count > 0;

        var requiredMinimumNights = property.MinimumNights;
        if (rateByDate.TryGetValue(checkIn, out var checkInRate) && checkInRate.MinimumStay is > 0)
            requiredMinimumNights = Math.Max(requiredMinimumNights, checkInRate.MinimumStay.Value);

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
        var petFee = petCount > 0 ? property.PetFeePerStay : 0m;
        var cleaningFee = await ResolveCleaningFeeAsync(slug, property.CleaningFee, cancellationToken)
            .ConfigureAwait(false);
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

    private async Task<decimal> ResolveCleaningFeeAsync(
        string slug,
        decimal configuredFee,
        CancellationToken cancellationToken)
    {
        var listing = await _db.RentalProperties
            .AsNoTracking()
            .Where(p => p.Slug == slug)
            .Select(p => (decimal?)p.CleaningFee)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return listing ?? configuredFee;
    }
}
