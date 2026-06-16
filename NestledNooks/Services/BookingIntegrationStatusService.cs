using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public interface IBookingIntegrationStatusService
{
    Task<BookingIntegrationStatusSnapshot> GetSnapshotAsync(
        string propertySlug,
        CancellationToken cancellationToken = default);
}

public sealed class BookingIntegrationStatusService(
    ApplicationDbContext db,
    IOptions<BookingOptions> bookingOptions,
    IOptions<PriceLabsOptions> priceLabsOptions) : IBookingIntegrationStatusService
{
    public async Task<BookingIntegrationStatusSnapshot> GetSnapshotAsync(
        string propertySlug,
        CancellationToken cancellationToken = default)
    {
        var slug = propertySlug.Trim().ToLowerInvariant();
        var property = bookingOptions.Value.Properties
            .FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        var airbnbUrl = property?.AirbnbIcalUrl?.Trim();
        var vrboUrl = property?.VrboIcalUrl?.Trim();

        var externalRows = await db.ExternalCalendarEvents
            .AsNoTracking()
            .Where(e => e.PropertySlug == slug)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var airbnbRows = externalRows.Where(e => e.Source.Equals("Airbnb", StringComparison.OrdinalIgnoreCase)).ToList();
        var vrboRows = externalRows.Where(e => e.Source.Equals("Vrbo", StringComparison.OrdinalIgnoreCase)).ToList();

        var rateStats = await db.PropertyNightlyRates
            .AsNoTracking()
            .Where(r => r.PropertySlug == slug)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), LastUpdatedUtc = g.Max(r => r.UpdatedAtUtc) })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var plOpts = priceLabsOptions.Value;
        var priceLabsConfigured = plOpts.Enabled
            && !string.IsNullOrWhiteSpace(plOpts.ApiKey)
            && !string.IsNullOrWhiteSpace(property?.PriceLabsListingId);

        return new BookingIntegrationStatusSnapshot
        {
            Airbnb = BuildCalendarChannel("Airbnb", airbnbUrl, airbnbRows.Count, MaxSyncedAt(airbnbRows)),
            Vrbo = BuildCalendarChannel("Vrbo", vrboUrl, vrboRows.Count, MaxSyncedAt(vrboRows)),
            PriceLabs = BuildPriceLabsChannel(priceLabsConfigured, rateStats?.Count ?? 0, rateStats?.LastUpdatedUtc),
        };
    }

    private static DateTime? MaxSyncedAt(IReadOnlyList<ExternalCalendarEvent> rows) =>
        rows.Count == 0 ? null : rows.Max(r => r.SyncedAtUtc);

    private static IntegrationChannelSnapshot BuildCalendarChannel(
        string name,
        string? url,
        int blockCount,
        DateTime? lastSyncedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new IntegrationChannelSnapshot(
                name,
                IsConfigured: false,
                IsSynced: false,
                blockCount,
                lastSyncedAtUtc,
                "iCal URL not configured in app settings.");
        }

        if (blockCount > 0)
        {
            return new IntegrationChannelSnapshot(
                name,
                IsConfigured: true,
                IsSynced: true,
                blockCount,
                lastSyncedAtUtc,
                $"{blockCount} blocked date range(s) imported.");
        }

        return new IntegrationChannelSnapshot(
            name,
            IsConfigured: true,
            IsSynced: false,
            blockCount,
            lastSyncedAtUtc,
            "URL configured but no blocks imported (check URL or sync logs).");
    }

    private static IntegrationChannelSnapshot BuildPriceLabsChannel(
        bool configured,
        int rateCount,
        DateTime? lastUpdatedUtc)
    {
        if (!configured)
        {
            return new IntegrationChannelSnapshot(
                "PriceLabs",
                IsConfigured: false,
                IsSynced: false,
                rateCount,
                lastUpdatedUtc,
                "Disabled or missing ApiKey / PriceLabsListingId.");
        }

        if (rateCount > 0)
        {
            return new IntegrationChannelSnapshot(
                "PriceLabs",
                IsConfigured: true,
                IsSynced: true,
                rateCount,
                lastUpdatedUtc,
                $"{rateCount} nightly rate(s) in database.");
        }

        return new IntegrationChannelSnapshot(
            "PriceLabs",
            IsConfigured: true,
            IsSynced: false,
            rateCount,
            lastUpdatedUtc,
            "Configured but no rates synced yet (check API key and listing ID).");
    }
}

public sealed class BookingIntegrationStatusSnapshot
{
    public required IntegrationChannelSnapshot Airbnb { get; init; }
    public required IntegrationChannelSnapshot Vrbo { get; init; }
    public required IntegrationChannelSnapshot PriceLabs { get; init; }
}

public sealed record IntegrationChannelSnapshot(
    string Name,
    bool IsConfigured,
    bool IsSynced,
    int ItemCount,
    DateTime? LastSyncedAtUtc,
    string Detail);
