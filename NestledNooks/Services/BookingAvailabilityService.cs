using Ical.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NestledNooks.Data;

namespace NestledNooks.Services;

public sealed class BookingAvailabilityService : IBookingAvailabilityService
{
    private readonly ApplicationDbContext _db;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly BookingOptions _options;
    private readonly GuestFacingCacheOptions _cacheOptions;
    private readonly ILogger<BookingAvailabilityService> _logger;

    public BookingAvailabilityService(
        ApplicationDbContext db,
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<BookingOptions> options,
        IOptions<GuestFacingCacheOptions> cacheOptions,
        ILogger<BookingAvailabilityService> logger)
    {
        _db = db;
        _dbFactory = dbFactory;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _options = options.Value;
        _cacheOptions = cacheOptions.Value;
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

        var cacheKey =
            $"{GuestDataCacheKeys.UnavailableDates(slug, from, to, excludeBookingId)}:v{GetAvailabilityCacheVersion(slug)}";
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<DateOnly>? cached) && cached is not null)
            return cached;

        var ranges = await GetBlockedRangesAsync(slug, excludeBookingId, cancellationToken).ConfigureAwait(false);
        var calendarSettings = await GetListingCalendarSettingsAsync(slug, cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var dates = new HashSet<DateOnly>();

        foreach (var (start, end, isExternal) in ranges)
        {
            if (isExternal && calendarSettings.ExternalCalendarTrustDays <= 0)
                continue;

            var cursor = start < from ? from : start;
            var lastNight = end.AddDays(-1);
            if (lastNight > to)
                lastNight = to;

            while (cursor <= lastNight)
            {
                if (!isExternal || cursor >= today)
                    dates.Add(cursor);

                cursor = cursor.AddDays(1);
            }
        }

        AddChannelHorizonUnavailableDates(
            dates,
            ranges,
            calendarSettings,
            today,
            from,
            to);

        var result = dates.OrderBy(d => d).ToList();
        _cache.Set(
            cacheKey,
            result,
            TimeSpan.FromMinutes(Math.Max(1, _cacheOptions.UnavailableDatesMinutes)));

        return result;
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

        var slug = NormalizeSlug(propertySlug);
        if (slug is null)
            return false;

        var ranges = await GetBlockedRangesAsync(slug, excludeBookingId, cancellationToken).ConfigureAwait(false);
        var calendarSettings = await GetListingCalendarSettingsAsync(slug, cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        foreach (var (blockCheckIn, blockCheckOut, isExternal) in ranges)
        {
            if (isExternal && calendarSettings.ExternalCalendarTrustDays <= 0)
                continue;

            if (!isExternal)
            {
                if (BookingStayDates.RangesOverlap(checkIn, checkOut, blockCheckIn, blockCheckOut))
                    return false;

                continue;
            }

            if (BookingStayDates.ExternalBlockAffectsStay(checkIn, checkOut, blockCheckIn, blockCheckOut))
                return false;
        }

        if (calendarSettings.ExternalCalendarTrustDays > 0)
        {
            var externalRanges = ranges
                .Where(r => r.IsExternal)
                .Select(r => (r.Start, r.End))
                .ToList();

            var closureStart = BookingStayDates.FindChannelClosureStart(externalRanges, today);
            if (closureStart is { } closedFrom &&
                BookingStayDates.RangesOverlap(checkIn, checkOut, closedFrom, closedFrom.AddYears(5)))
            {
                return false;
            }

            if (!calendarSettings.AllowFarAdvanceDirectBooking)
            {
                var trustThrough = today.AddDays(calendarSettings.ExternalCalendarTrustDays);
                if (checkIn > trustThrough)
                    return false;
            }
        }

        return true;
    }

    public async Task SyncExternalCalendarsAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        if (!force && !await NeedsExternalCalendarSyncAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogDebug(
                "External calendar sync skipped — imported blocks are newer than {Minutes} minutes.",
                _options.CalendarSyncIntervalMinutes);
            return;
        }

        var client = _httpClientFactory.CreateClient("CalendarSync");
        var syncedAt = DateTime.UtcNow;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

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

            var existing = await db.ExternalCalendarEvents
                .Where(e => e.PropertySlug == slug)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            db.ExternalCalendarEvents.RemoveRange(existing);
            db.ExternalCalendarEvents.AddRange(imported);

            _logger.LogInformation(
                "Imported {Count} external calendar blocks for {Property} (Airbnb/Vrbo).",
                imported.Count,
                slug);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        foreach (var property in _options.Properties)
        {
            if (string.IsNullOrWhiteSpace(property.Slug))
                continue;

            BumpAvailabilityCacheVersion(NormalizeSlug(property.Slug)!);
        }

        _logger.LogInformation("External calendar sync completed.");
    }

    private async Task<bool> NeedsExternalCalendarSyncAsync(CancellationToken cancellationToken)
    {
        var configuredSlugs = _options.Properties
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.Slug) &&
                (!string.IsNullOrWhiteSpace(p.AirbnbIcalUrl) || !string.IsNullOrWhiteSpace(p.VrboIcalUrl)))
            .Select(p => NormalizeSlug(p.Slug)!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (configuredSlugs.Count == 0)
            return false;

        var threshold = DateTime.UtcNow.AddMinutes(-Math.Max(30, _options.CalendarSyncIntervalMinutes));

        foreach (var slug in configuredSlugs)
        {
            var latest = await _db.ExternalCalendarEvents
                .AsNoTracking()
                .Where(e => e.PropertySlug == slug)
                .MaxAsync(e => (DateTime?)e.SyncedAtUtc, cancellationToken)
                .ConfigureAwait(false);

            if (latest is null || latest < threshold)
                return true;
        }

        return false;
    }

    private long GetAvailabilityCacheVersion(string slug)
    {
        var key = $"guest:availability-version:{slug}";
        return _cache.TryGetValue(key, out long version) ? version : 0;
    }

    private void BumpAvailabilityCacheVersion(string slug) =>
        _cache.Set($"guest:availability-version:{slug}", DateTime.UtcNow.Ticks, TimeSpan.FromHours(24));

    private async Task<List<(DateOnly Start, DateOnly End, bool IsExternal)>> GetBlockedRangesAsync(
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

        var ranges = new List<(DateOnly Start, DateOnly End, bool IsExternal)>();
        ranges.AddRange(external.Select(e => (e.StartDate, e.EndDate, true)));
        ranges.AddRange(local.Select(b => (b.CheckIn, b.CheckOut, false)));
        return ranges;
    }

    private async Task<ListingCalendarSettings> GetListingCalendarSettingsAsync(
        string propertySlug,
        CancellationToken cancellationToken)
    {
        var row = await _db.RentalProperties
            .AsNoTracking()
            .Where(p => p.Slug == propertySlug)
            .Select(p => new { p.ExternalCalendarTrustDays, p.AllowFarAdvanceDirectBooking })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ListingCalendarSettings(
            ListingSettingsDefaults.ClampExternalCalendarTrustDays(
                row?.ExternalCalendarTrustDays ?? ListingSettingsDefaults.ExternalCalendarTrustDays),
            row?.AllowFarAdvanceDirectBooking ?? ListingSettingsDefaults.AllowFarAdvanceDirectBooking);
    }

    private static void AddChannelHorizonUnavailableDates(
        HashSet<DateOnly> dates,
        IReadOnlyList<(DateOnly Start, DateOnly End, bool IsExternal)> ranges,
        ListingCalendarSettings calendarSettings,
        DateOnly today,
        DateOnly from,
        DateOnly to)
    {
        if (calendarSettings.ExternalCalendarTrustDays <= 0)
            return;

        var externalRanges = ranges
            .Where(r => r.IsExternal)
            .Select(r => (r.Start, r.End))
            .ToList();

        var closureStart = BookingStayDates.FindChannelClosureStart(externalRanges, today);
        if (closureStart is { } closedFrom)
            AddUnavailableRange(dates, closedFrom, to, from, to);

        if (!calendarSettings.AllowFarAdvanceDirectBooking)
        {
            var closedAfterTrust = today.AddDays(calendarSettings.ExternalCalendarTrustDays + 1);
            AddUnavailableRange(dates, closedAfterTrust, to, from, to);
        }
    }

    private static void AddUnavailableRange(
        HashSet<DateOnly> dates,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        DateOnly from,
        DateOnly to)
    {
        var cursor = rangeStart < from ? from : rangeStart;
        var last = rangeEnd < to ? rangeEnd : to;

        while (cursor <= last)
        {
            dates.Add(cursor);
            cursor = cursor.AddDays(1);
        }
    }

    private sealed record ListingCalendarSettings(int ExternalCalendarTrustDays, bool AllowFarAdvanceDirectBooking);

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
