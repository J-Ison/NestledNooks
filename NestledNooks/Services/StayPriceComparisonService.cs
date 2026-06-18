using Microsoft.Extensions.Options;
using NestledNooks.Models;

namespace NestledNooks.Services;

public sealed class StayPriceComparisonService(
    BookingPricingService pricing,
    IPriceLabsApiClient priceLabsApi,
    IPriceLabsChannelListingResolver channelResolver,
    IOptions<PriceLabsOptions> priceLabsOptions,
    ILogger<StayPriceComparisonService> logger) : IStayPriceComparisonService
{
    public async Task<StayPriceComparison> GetComparisonAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int petCount = 0,
        CancellationToken cancellationToken = default)
    {
        var direct = await pricing.CalculateAsync(propertySlug, checkIn, checkOut, petCount, cancellationToken)
            .ConfigureAwait(false);

        var property = pricing.GetProperty(propertySlug)
            ?? throw new InvalidOperationException("Unknown property.");

        var listing = await pricing.GetListingSettingsAsync(propertySlug, cancellationToken).ConfigureAwait(false);
        var nights = checkOut.DayNumber - checkIn.DayNumber;

        var airbnb = await TryChannelEstimateAsync(
            property,
            listing,
            checkIn,
            checkOut,
            nights,
            petCount,
            channelPms: "airbnb",
            channelLabel: "Airbnb",
            cleaningFee: listing.CleaningFee,
            guestServiceFeePercent: priceLabsOptions.Value.DefaultAirbnbGuestServiceFeePercent,
            occupancyTaxPercent: 0m,
            cancellationToken)
            .ConfigureAwait(false);

        var vrbo = await TryChannelEstimateAsync(
            property,
            listing,
            checkIn,
            checkOut,
            nights,
            petCount,
            channelPms: "vrbo",
            channelLabel: "Vrbo",
            cleaningFee: listing.CleaningFee,
            guestServiceFeePercent: priceLabsOptions.Value.DefaultVrboGuestServiceFeePercent,
            occupancyTaxPercent: 0m,
            cancellationToken)
            .ConfigureAwait(false);

        return new StayPriceComparison(direct, airbnb, vrbo);
    }

    private async Task<ChannelPriceEstimate> TryChannelEstimateAsync(
        PropertyBookingOptions property,
        PropertyListingSettings listing,
        DateOnly checkIn,
        DateOnly checkOut,
        int nights,
        int petCount,
        string channelPms,
        string channelLabel,
        decimal cleaningFee,
        decimal guestServiceFeePercent,
        decimal occupancyTaxPercent,
        CancellationToken cancellationToken)
    {
        var opts = priceLabsOptions.Value;

        if (!opts.Enabled || string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            return ChannelPriceEstimate.Unavailable(
                channelLabel,
                "Channel estimate unavailable");
        }

        var channelListing = await channelResolver
            .ResolveAsync(property, channelPms, cancellationToken)
            .ConfigureAwait(false);

        if (channelListing is null)
        {
            return ChannelPriceEstimate.Unavailable(
                channelLabel,
                $"No {channelLabel} listing in PriceLabs");
        }

        try
        {
            var prices = await priceLabsApi
                .GetListingPricesAsync(
                    channelListing.ListingId,
                    channelListing.Pms,
                    checkIn,
                    checkOut,
                    cancellationToken)
                .ConfigureAwait(false);

            if (prices.Count == 0)
            {
                return ChannelPriceEstimate.Unavailable(
                    channelLabel,
                    "No channel rates for these dates");
            }

            var rateByDate = prices
                .GroupBy(p => p.Date)
                .ToDictionary(g => g.Key, g => g.Last().Rate);

            var lodgingSubtotal = SumNightlySubtotal(checkIn, checkOut, rateByDate, property.NightlyRate);
            var petFee = ListingSettingsDefaults.CalculatePetDeposit(petCount, listing.PetDepositPerTwoPets);
            var beforeTax = lodgingSubtotal + cleaningFee + petFee;
            var guestServiceFee = Math.Round(
                beforeTax * guestServiceFeePercent / 100m,
                2,
                MidpointRounding.AwayFromZero);
            var taxableAmount = beforeTax + guestServiceFee;
            var occupancyTax = Math.Round(
                taxableAmount * occupancyTaxPercent / 100m,
                2,
                MidpointRounding.AwayFromZero);
            var total = beforeTax + guestServiceFee + occupancyTax;
            var averageNightly = Math.Round(lodgingSubtotal / nights, 2, MidpointRounding.AwayFromZero);

            var noteParts = new List<string>();
            if (guestServiceFeePercent > 0)
                noteParts.Add("est. service fee");
            if (occupancyTaxPercent > 0)
                noteParts.Add("est. taxes");

            var note = noteParts.Count > 0
                ? $"Incl. {string.Join(" & ", noteParts)}"
                : null;

            return new ChannelPriceEstimate(
                channelLabel,
                total,
                averageNightly,
                UsesLiveRates: true,
                Note: note);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to fetch {Channel} rates from PriceLabs for {Slug} (listing {ListingId}, pms {Pms}).",
                channelLabel,
                property.Slug,
                channelListing.ListingId,
                channelListing.Pms);

            return ChannelPriceEstimate.Unavailable(
                channelLabel,
                "Could not load estimate");
        }
    }

    private static decimal SumNightlySubtotal(
        DateOnly checkIn,
        DateOnly checkOut,
        IReadOnlyDictionary<DateOnly, decimal> rateByDate,
        decimal fallbackNightlyRate)
    {
        decimal subtotal = 0;

        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            subtotal += rateByDate.TryGetValue(date, out var rate)
                ? rate
                : fallbackNightlyRate;
        }

        return subtotal;
    }
}
