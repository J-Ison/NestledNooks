using Ical.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class BookingAvailabilityServiceTests
{
    private const string SampleIcal = """
        BEGIN:VCALENDAR
        PRODID:-//Airbnb Inc//Hosting Calendar 1.0//EN
        CALSCALE:GREGORIAN
        VERSION:2.0
        BEGIN:VEVENT
        DTSTAMP:20260613T051203Z
        DTSTART;VALUE=DATE:20260624
        DTEND;VALUE=DATE:20260701
        SUMMARY:Reserved
        UID:test@airbnb.com
        END:VEVENT
        BEGIN:VEVENT
        DTSTAMP:20260613T051203Z
        DTSTART;VALUE=DATE:20260706
        DTEND;VALUE=DATE:20260712
        SUMMARY:Reserved
        UID:test2@airbnb.com
        END:VEVENT
        END:VCALENDAR
        """;

    [Fact]
    public void IcalNet_parses_airbnb_date_events()
    {
        var calendar = Calendar.Load(SampleIcal);
        Assert.Equal(2, calendar.Events.Count);

        var first = calendar.Events.First();
        Assert.NotNull(first.Start);
        Assert.False(first.Start.HasTime);
        Assert.Equal(new DateTime(2026, 6, 24), first.Start.Date);
        Assert.NotNull(first.End);
        Assert.Equal(new DateTime(2026, 7, 1), first.End.Date);
    }

    [Fact]
    public async Task SyncExternalCalendarsAsync_imports_ical_blocks()
    {
        var db = await CreateDbAsync();
        var handler = new FakeCalendarHandler(SampleIcal);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<IOptions<BookingOptions>>(Options.Create(new BookingOptions
        {
            Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = "deerfield-retreat",
                    AirbnbIcalUrl = "https://example.test/airbnb.ics"
                }
            ]
        }));
        services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(handler));
        services.AddSingleton<IBookingAvailabilityService, BookingAvailabilityService>();

        var service = services.BuildServiceProvider().GetRequiredService<IBookingAvailabilityService>();
        await service.SyncExternalCalendarsAsync();

        var events = await db.ExternalCalendarEvents.ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal("deerfield-retreat", e.PropertySlug));

        var unavailable = await service.GetUnavailableDatesAsync(
            "deerfield-retreat",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 7, 31));

        Assert.Contains(new DateOnly(2026, 6, 24), unavailable);
        Assert.Contains(new DateOnly(2026, 6, 30), unavailable);
        Assert.DoesNotContain(new DateOnly(2026, 7, 1), unavailable);
        Assert.Contains(new DateOnly(2026, 7, 6), unavailable);
        Assert.Contains(new DateOnly(2026, 7, 11), unavailable);
        Assert.DoesNotContain(new DateOnly(2026, 7, 12), unavailable);
    }

    [Fact]
    public async Task SyncExternalCalendarsAsync_keeps_existing_when_fetch_fails()
    {
        var db = await CreateDbAsync();
        db.ExternalCalendarEvents.Add(new ExternalCalendarEvent
        {
            PropertySlug = "deerfield-retreat",
            Source = "Airbnb",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 5),
            SyncedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new FakeCalendarHandler("", statusCode: System.Net.HttpStatusCode.NotFound);
        var service = CreateService(db, handler);
        await service.SyncExternalCalendarsAsync();

        var events = await db.ExternalCalendarEvents.ToListAsync();
        Assert.Single(events);
        Assert.Equal(new DateOnly(2026, 8, 1), events[0].StartDate);
    }

    private static BookingAvailabilityService CreateService(ApplicationDbContext db, FakeCalendarHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddSingleton<IOptions<BookingOptions>>(Options.Create(new BookingOptions
        {
            Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = "deerfield-retreat",
                    AirbnbIcalUrl = "https://example.test/airbnb.ics"
                }
            ]
        }));
        services.AddSingleton<IHttpClientFactory>(new FakeHttpClientFactory(handler));
        services.AddSingleton<BookingAvailabilityService>();
        return services.BuildServiceProvider().GetRequiredService<BookingAvailabilityService>();
    }

    private static async Task<ApplicationDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
        return db;
    }

    private sealed class FakeHttpClientFactory(FakeCalendarHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class FakeCalendarHandler(string icalBody, System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (statusCode == System.Net.HttpStatusCode.OK)
            {
                return Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(icalBody)
                });
            }

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
