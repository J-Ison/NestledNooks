using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class PriceLabsPricingSyncService(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    IPriceLabsApiClient apiClient,
    IOptions<BookingOptions> bookingOptions,
    IOptions<PriceLabsOptions> priceLabsOptions,
    ILogger<PriceLabsPricingSyncService> logger) : IPriceLabsPricingSyncService
{
    public async Task SyncAllConfiguredPropertiesAsync(bool force = false, CancellationToken cancellationToken = default)
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

        var syncStaleBefore = DateTime.UtcNow.AddMinutes(-Math.Max(30, opts.SyncIntervalMinutes));
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
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
                var slug = property.Slug.Trim().ToLowerInvariant();

                if (!force)
                {
                    var latest = await db.PropertyNightlyRates
                        .AsNoTracking()
                        .Where(r => r.PropertySlug == slug)
                        .MaxAsync(r => (DateTime?)r.UpdatedAtUtc, cancellationToken)
                        .ConfigureAwait(false);

                    if (latest is not null && latest >= syncStaleBefore)
                    {
                        logger.LogDebug(
                            "PriceLabs sync skipped for {Slug} — rates updated within the last {Minutes} minutes.",
                            slug,
                            opts.SyncIntervalMinutes);
                        continue;
                    }
                }

                await SyncPropertyAsync(
                    db,
                    slug,
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
        ApplicationDbContext db,
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

        var pricesByDate = prices
            .GroupBy(p => p.Date)
            .ToDictionary(g => g.Key, g => g.Last());

        var syncedDates = pricesByDate.Keys.ToHashSet();
        var existingRows = await db.PropertyNightlyRates
            .Where(r => r.PropertySlug == slug &&
                        (syncedDates.Contains(r.Date) || (r.Date >= startDate && r.Date <= endDate)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingByDate = existingRows.ToDictionary(r => r.Date);

        foreach (var day in pricesByDate.Values)
        {
            if (existingByDate.TryGetValue(day.Date, out var row))
            {
                row.Rate = day.Rate;
                row.MinimumStay = day.MinimumStay;
                row.UpdatedAtUtc = syncedAt;
            }
            else
            {
                var added = new PropertyNightlyRate
                {
                    PropertySlug = slug,
                    Date = day.Date,
                    Rate = day.Rate,
                    MinimumStay = day.MinimumStay,
                    UpdatedAtUtc = syncedAt,
                };
                db.PropertyNightlyRates.Add(added);
                existingByDate[day.Date] = added;
            }
        }

        foreach (var stale in existingRows.Where(r => !syncedDates.Contains(r.Date)))
            db.PropertyNightlyRates.Remove(stale);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsDuplicatePropertyRate(ex))
        {
            db.ChangeTracker.Clear();
            logger.LogWarning(
                ex,
                "PriceLabs rate upsert raced for {Slug}; another sync already wrote overlapping dates.",
                slug);
        }

        logger.LogInformation(
            "PriceLabs synced {Count} nightly rates for {Slug} ({Start} to {End}).",
            pricesByDate.Count,
            slug,
            startDate,
            endDate);
    }

    private static bool IsDuplicatePropertyRate(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_PropertyNightlyRates_PropertySlug_Date", StringComparison.OrdinalIgnoreCase)
            || (message.Contains("PropertyNightlyRates", StringComparison.OrdinalIgnoreCase)
                && message.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }
}
