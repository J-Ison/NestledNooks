using Microsoft.Extensions.Options;
using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed class BookingPricingService
{
    private readonly BookingOptions _options;

    public BookingPricingService(IOptions<BookingOptions> options) => _options = options.Value;

    public IReadOnlyList<PropertyBookingOptions> GetAllProperties() => _options.Properties;

    public PropertyBookingOptions? GetProperty(string slug) =>
        _options.Properties.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public BookingQuote Calculate(string propertySlug, DateOnly checkIn, DateOnly checkOut, int petCount)
    {
        var property = GetProperty(propertySlug)
            ?? throw new InvalidOperationException("Unknown property.");

        var nights = checkOut.DayNumber - checkIn.DayNumber;
        if (nights < 1)
            throw new InvalidOperationException("Stay must be at least one night.");

        if (nights < property.MinimumNights)
            throw new InvalidOperationException($"Minimum stay is {property.MinimumNights} nights.");

        var subtotal = nights * property.NightlyRate;
        var petFee = petCount > 0 ? property.PetFeePerStay : 0m;
        var total = subtotal + property.CleaningFee + petFee;

        return new BookingQuote(nights, property.NightlyRate, subtotal, property.CleaningFee, petFee, total);
    }
}
