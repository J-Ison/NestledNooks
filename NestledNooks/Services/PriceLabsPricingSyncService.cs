using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class PriceLabsPricingSyncService(
    ApplicationDbContext db,
    IPriceLabsApiClient apiClient,
    IOptions<BookingOptions> bookingOptions,
    IOptions<PriceLabsOptions> priceLabsOptions,
    ILogger<PriceLabsPricingSyncService> logger) : IPriceLabsPricingSyncService
{
    public async Task SyncAllConfiguredPropertiesAsync(CancellationToken cancellationToken = default)
    {
        var opts = priceLabsOptions.Value;
        if (!opts.Enabled)
        {
            logger.LogDebug("PriceLabs sync skipped — integration disabled.");
            return;
        }

        if (string.IsNullOrWhiteSpace(opts.ApiKey))
        {
            logger.LogWarning("PriceLabs sync skipped — ApiKey is not configured.");
            return;
        }

        var properties = bookingOptions.Value.Properties
            .Where(p => !string.IsNullOrWhiteSpace(p.Slug) && !string.IsNullOrWhiteSpace(p.PriceLabsListingId))
            .ToList();

        if (properties.Count == 0)
        {
            logger.LogInformation("PriceLabs sync skipped — no properties have PriceLabsListingId configured.");
            return;
        }

        var listingPmsById = await BuildListingPmsMapAsync(cancellationToken).ConfigureAwait(false);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var daysAhead = Math.Clamp(opts.SyncDaysAhead, 30, 720);
        var endDate = startDate.AddDays(daysAhead);
        var syncedAt = DateTime.UtcNow;

        foreach (var property in properties)
        {
            var listingId = property.PriceLabsListingId!.Trim();
            var pms = property.PriceLabsPms?.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(pms))
            {
                if (!listingPmsById.TryGetValue(listingId, out pms))
                {
                    logger.LogWarning(
                        "PriceLabs sync skipped for {Slug} — could not resolve PMS for listing {ListingId}. " +
                        "Set Booking:Properties:0:PriceLabsPms or verify the listing ID.",
                        property.Slug,
                        listingId);
                    continue;
                }
            }

            try
            {
                await SyncPropertyAsync(
                    property.Slug.Trim().ToLowerInvariant(),
                    listingId,
                    pms,
                    startDate,
                    endDate,
                    syncedAt,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "PriceLabs sync failed for property {Slug} (listing {ListingId}, pms {Pms}).",
                    property.Slug,
                    listingId,
                    pms);
            }
        }
    }

    private async Task<Dictionary<string, string>> BuildListingPmsMapAsync(CancellationToken cancellationToken)
    {
        var listings = await apiClient.GetListingsAsync(cancellationToken).ConfigureAwait(false);
        return listings
            .Where(l => !string.IsNullOrWhiteSpace(l.ListingId) && !string.IsNullOrWhiteSpace(l.Pms))
            .GroupBy(l => l.ListingId.Trim(), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Pms!.Trim().ToLowerInvariant(), StringComparer.Ordinal);
    }

    private async Task SyncPropertyAsync(
        string slug,
        string listingId,
        string pms,
        DateOnly startDate,
        DateOnly endDate,
        DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        var prices = await apiClient
            .GetListingPricesAsync(listingId, pms, startDate, endDate, cancellationToken)
            .ConfigureAwait(false);

        if (prices.Count == 0)
        {
            logger.LogWarning(
                "PriceLabs returned no prices for {Slug} (listing {ListingId}, pms {Pms}).",
                slug,
                listingId,
                pms);
            return;
        }

        var existingRows = await db.PropertyNightlyRates
            .Where(r => r.PropertySlug == slug && r.Date >= startDate && r.Date <= endDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingByDate = existingRows.ToDictionary(r => r.Date);
        var syncedDates = new HashSet<DateOnly>();

        foreach (var day in prices)
        {
            syncedDates.Add(day.Date);

            if (existingByDate.TryGetValue(day.Date, out var row))
            {
                row.Rate = day.Rate;
                row.MinimumStay = day.MinimumStay;
                row.UpdatedAtUtc = syncedAt;
            }
            else
            {
                db.PropertyNightlyRates.Add(new PropertyNightlyRate
                {
                    PropertySlug = slug,
                    Date = day.Date,
                    Rate = day.Rate,
                    MinimumStay = day.MinimumStay,
                    UpdatedAtUtc = syncedAt,
                });
            }
        }

        foreach (var stale in existingRows.Where(r => !syncedDates.Contains(r.Date)))
            db.PropertyNightlyRates.Remove(stale);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "PriceLabs synced {Count} nightly rates for {Slug} ({Start} to {End}).",
            prices.Count,
            slug,
            startDate,
            endDate);
    }
}
