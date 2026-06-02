using Ical.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class BookingAvailabilityService : IBookingAvailabilityService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BookingOptions _options;
    private readonly ILogger<BookingAvailabilityService> _logger;

    public BookingAvailabilityService(
        ApplicationDbContext db,
        IHttpClientFactory httpClientFactory,
        IOptions<BookingOptions> options,
        ILogger<BookingAvailabilityService> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DateOnly>> GetUnavailableDatesAsync(
        string propertySlug,
        DateOnly from,
        DateOnly to,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default)
    {
        var ranges = await GetBlockedRangesAsync(propertySlug, excludeBookingId, cancellationToken).ConfigureAwait(false);
        var dates = new HashSet<DateOnly>();

        foreach (var (start, end) in ranges)
        {
            var cursor = start < from ? from : start;
            var lastNight = end.AddDays(-1);
            if (lastNight > to)
                lastNight = to;

            while (cursor <= lastNight)
            {
                dates.Add(cursor);
                cursor = cursor.AddDays(1);
            }
        }

        return dates.OrderBy(d => d).ToList();
    }

    public async Task<bool> IsRangeAvailableAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default)
    {
        if (checkOut <= checkIn)
            return false;

        var unavailable = await GetUnavailableDatesAsync(
            propertySlug,
            checkIn,
            checkOut.AddDays(-1),
            excludeBookingId,
            cancellationToken).ConfigureAwait(false);

        return unavailable.Count == 0;
    }

    public async Task SyncExternalCalendarsAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("CalendarSync");
        var syncedAt = DateTime.UtcNow;

        foreach (var property in _options.Properties)
        {
            if (string.IsNullOrWhiteSpace(property.Slug))
                continue;

            var existing = await _db.ExternalCalendarEvents
                .Where(e => e.PropertySlug == property.Slug)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _db.ExternalCalendarEvents.RemoveRange(existing);

            var imported = new List<ExternalCalendarEvent>();

            if (!string.IsNullOrWhiteSpace(property.AirbnbIcalUrl))
            {
                var events = await FetchIcalEventsAsync(
                    client, property.Slug, "Airbnb", property.AirbnbIcalUrl, syncedAt, cancellationToken);
                imported.AddRange(events);
            }

            if (!string.IsNullOrWhiteSpace(property.VrboIcalUrl))
            {
                var events = await FetchIcalEventsAsync(
                    client, property.Slug, "Vrbo", property.VrboIcalUrl, syncedAt, cancellationToken);
                imported.AddRange(events);
            }

            if (imported.Count > 0)
                _db.ExternalCalendarEvents.AddRange(imported);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("External calendar sync completed.");
    }

    private async Task<List<(DateOnly Start, DateOnly End)>> GetBlockedRangesAsync(
        string propertySlug,
        int? excludeBookingId,
        CancellationToken cancellationToken)
    {
        var external = await _db.ExternalCalendarEvents
            .AsNoTracking()
            .Where(e => e.PropertySlug == propertySlug)
            .Select(e => new { e.StartDate, e.EndDate })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var localQuery = _db.BookingRequests
            .AsNoTracking()
            .Where(b => b.PropertySlug == propertySlug && BookingStatuses.DateHolding.Contains(b.Status));

        if (excludeBookingId is int id)
            localQuery = localQuery.Where(b => b.Id != id);

        var local = await localQuery
            .Select(b => new { b.CheckIn, b.CheckOut })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ranges = new List<(DateOnly Start, DateOnly End)>();
        ranges.AddRange(external.Select(e => (e.StartDate, e.EndDate)));
        ranges.AddRange(local.Select(b => (b.CheckIn, b.CheckOut)));
        return ranges;
    }

    private async Task<List<ExternalCalendarEvent>> FetchIcalEventsAsync(
        HttpClient client,
        string propertySlug,
        string source,
        string url,
        DateTime syncedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var icalText = await client.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
            var calendar = Calendar.Load(icalText);
            var results = new List<ExternalCalendarEvent>();

            foreach (var evt in calendar.Events)
            {
                if (evt.Start.Value == default)
                    continue;

                var start = DateOnly.FromDateTime(evt.Start.Value);
                var end = evt.End.Value != default
                    ? DateOnly.FromDateTime(evt.End.Value)
                    : start.AddDays(1);

                if (end <= start)
                    end = start.AddDays(1);

                results.Add(new ExternalCalendarEvent
                {
                    PropertySlug = propertySlug,
                    Source = source,
                    StartDate = start,
                    EndDate = end,
                    Summary = evt.Summary,
                    SyncedAtUtc = syncedAt
                });
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync {Source} iCal for {Property}", source, propertySlug);
            return [];
        }
    }
}
