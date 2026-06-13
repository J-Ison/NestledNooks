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
        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return [];

        var ranges = await GetBlockedRangesAsync(slug, excludeBookingId, cancellationToken).ConfigureAwait(false);
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

            var slug = NormalizeSlug(property.Slug)!;
            var imported = new List<ExternalCalendarEvent>();

            if (!string.IsNullOrWhiteSpace(property.AirbnbIcalUrl))
            {
                var events = await FetchIcalEventsAsync(
                    client, slug, "Airbnb", property.AirbnbIcalUrl, syncedAt, cancellationToken);
                imported.AddRange(events);
            }

            if (!string.IsNullOrWhiteSpace(property.VrboIcalUrl))
            {
                var events = await FetchIcalEventsAsync(
                    client, slug, "Vrbo", property.VrboIcalUrl, syncedAt, cancellationToken);
                imported.AddRange(events);
            }

            if (imported.Count == 0)
            {
                _logger.LogWarning(
                    "No external calendar blocks imported for {Property}. Keeping existing cached blocks.",
                    slug);
                continue;
            }

            var existing = await _db.ExternalCalendarEvents
                .Where(e => e.PropertySlug == slug)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            _db.ExternalCalendarEvents.RemoveRange(existing);
            _db.ExternalCalendarEvents.AddRange(imported);

            _logger.LogInformation(
                "Imported {Count} external calendar blocks for {Property} (Airbnb/Vrbo).",
                imported.Count,
                slug);
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
                if (evt.Start is null)
                    continue;

                var start = ToBlockedDate(evt.Start);
                if (start is null)
                    continue;

                var end = evt.End is not null
                    ? ToBlockedDate(evt.End) ?? start.Value.AddDays(1)
                    : start.Value.AddDays(1);

                if (end <= start)
                    end = start.Value.AddDays(1);

                results.Add(new ExternalCalendarEvent
                {
                    PropertySlug = propertySlug,
                    Source = source,
                    StartDate = start.Value,
                    EndDate = end,
                    Summary = evt.Summary,
                    SyncedAtUtc = syncedAt
                });
            }

            _logger.LogInformation(
                "Parsed {Count} {Source} iCal events for {Property}.",
                results.Count,
                source,
                propertySlug);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync {Source} iCal for {Property}", source, propertySlug);
            return [];
        }
    }

    private static string? NormalizeSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        return slug.Trim().ToLowerInvariant();
    }

    private static DateOnly? ToBlockedDate(Ical.Net.DataTypes.IDateTime dateTime)
    {
        if (dateTime is null)
            return null;

        // All-day Airbnb/Vrbo events use DATE values; use the calendar date without timezone drift.
        if (!dateTime.HasTime)
            return DateOnly.FromDateTime(dateTime.Date);

        var local = dateTime.AsSystemLocal;
        return DateOnly.FromDateTime(local.Date);
    }
}
