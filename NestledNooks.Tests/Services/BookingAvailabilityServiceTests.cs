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
        var connection = await OpenConnectionAsync();
        var handler = new FakeCalendarHandler(SampleIcal);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(connection);
        services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
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
        services.AddScoped<IBookingAvailabilityService, BookingAvailabilityService>();

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var service = scope.ServiceProvider.GetRequiredService<IBookingAvailabilityService>();
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

    [Fact]
    public async Task GetUnavailableDatesAsync_applies_external_blocks_beyond_trust_horizon()
    {
        var db = await CreateDbAsync();
        var property = PropertySeedData.CreateDeerfieldRetreat();
        property.ExternalCalendarTrustDays = 180;
        property.AllowFarAdvanceDirectBooking = true;
        db.RentalProperties.Add(property);
        var blockStart = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(200);
        db.ExternalCalendarEvents.Add(new ExternalCalendarEvent
        {
            PropertySlug = "deerfield-retreat",
            Source = "Airbnb",
            StartDate = blockStart,
            EndDate = blockStart.AddDays(10),
            SyncedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var unavailable = await service.GetUnavailableDatesAsync(
            "deerfield-retreat",
            DateOnly.FromDateTime(DateTime.UtcNow.Date),
            DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(400));

        Assert.Contains(blockStart, unavailable);
        Assert.Contains(blockStart.AddDays(9), unavailable);
    }

    [Fact]
    public async Task GetUnavailableDatesAsync_marks_channel_closure_after_last_airbnb_window()
    {
        var db = await CreateDbAsync();
        var property = PropertySeedData.CreateDeerfieldRetreat();
        property.ExternalCalendarTrustDays = 180;
        property.AllowFarAdvanceDirectBooking = true;
        db.RentalProperties.Add(property);
        var closureStart = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(40);
        db.ExternalCalendarEvents.Add(new ExternalCalendarEvent
        {
            PropertySlug = "deerfield-retreat",
            Source = "Airbnb",
            StartDate = closureStart,
            EndDate = closureStart.AddDays(120),
            SyncedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var unavailable = await service.GetUnavailableDatesAsync(
            "deerfield-retreat",
            closureStart,
            closureStart.AddDays(30));

        Assert.All(Enumerable.Range(0, 31), offset =>
            Assert.Contains(closureStart.AddDays(offset), unavailable));
    }

    [Fact]
    public async Task GetUnavailableDatesAsync_grays_out_dates_past_trust_when_far_advance_disabled()
    {
        var db = await CreateDbAsync();
        var property = PropertySeedData.CreateDeerfieldRetreat();
        property.ExternalCalendarTrustDays = 180;
        property.AllowFarAdvanceDirectBooking = false;
        db.RentalProperties.Add(property);
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var beyondTrust = today.AddDays(200);
        var unavailable = await service.GetUnavailableDatesAsync(
            "deerfield-retreat",
            beyondTrust,
            beyondTrust.AddDays(5));

        Assert.Contains(beyondTrust, unavailable);
    }

    [Fact]
    public async Task GetUnavailableDatesAsync_applies_external_blocks_within_trust_horizon()
    {
        var db = await CreateDbAsync();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow.Date).AddDays(30);
        var property = PropertySeedData.CreateDeerfieldRetreat();
        property.ExternalCalendarTrustDays = 180;
        db.RentalProperties.Add(property);
        db.ExternalCalendarEvents.Add(new ExternalCalendarEvent
        {
            PropertySlug = "deerfield-retreat",
            Source = "Airbnb",
            StartDate = checkIn,
            EndDate = checkIn.AddDays(3),
            SyncedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var unavailable = await service.GetUnavailableDatesAsync(
            "deerfield-retreat",
            checkIn,
            checkIn.AddDays(5));

        Assert.Contains(checkIn, unavailable);
        Assert.Contains(checkIn.AddDays(1), unavailable);
        Assert.Contains(checkIn.AddDays(2), unavailable);
        Assert.DoesNotContain(checkIn.AddDays(3), unavailable);
    }

    [Fact]
    public async Task IsRangeAvailableAsync_rejects_stay_that_wraps_existing_booking()
    {
        var db = await CreateDbAsync();
        var property = PropertySeedData.CreateDeerfieldRetreat();
        db.RentalProperties.Add(property);
        db.BookingRequests.Add(new BookingRequest
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            GuestFullName = "Guest",
            GuestEmail = "guest@test.com",
            CheckIn = new DateOnly(2026, 8, 4),
            CheckOut = new DateOnly(2026, 8, 14),
            GuestCount = 2,
            PetCount = 0,
            NightCount = 10,
            NightlyRate = 200m,
            CleaningFee = 200m,
            PetFee = 0m,
            Subtotal = 2000m,
            TotalAmount = 2200m,
            Status = BookingStatuses.Approved,
            PaymentStatus = PaymentStatuses.Unpaid,
            BookingNumber = "NN-TEST-WRAP",
            CreatedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var available = await service.IsRangeAvailableAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 8, 3),
            new DateOnly(2026, 8, 15));

        Assert.False(available);
    }

    [Fact]
    public async Task IsRangeAvailableAsync_allows_adjacent_stay_before_existing_booking()
    {
        var db = await CreateDbAsync();
        var property = PropertySeedData.CreateDeerfieldRetreat();
        db.RentalProperties.Add(property);
        db.BookingRequests.Add(new BookingRequest
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            GuestFullName = "Guest",
            GuestEmail = "guest@test.com",
            CheckIn = new DateOnly(2026, 8, 4),
            CheckOut = new DateOnly(2026, 8, 14),
            GuestCount = 2,
            PetCount = 0,
            NightCount = 10,
            NightlyRate = 200m,
            CleaningFee = 200m,
            PetFee = 0m,
            Subtotal = 2000m,
            TotalAmount = 2200m,
            Status = BookingStatuses.Approved,
            PaymentStatus = PaymentStatuses.Unpaid,
            BookingNumber = "NN-TEST-ADJ",
            CreatedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new FakeCalendarHandler(""));
        var available = await service.IsRangeAvailableAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4));

        Assert.True(available);
    }

    private static BookingAvailabilityService CreateService(ApplicationDbContext db, FakeCalendarHandler handler)
    {
        var connection = db.Database.GetDbConnection() as SqliteConnection
            ?? throw new InvalidOperationException("Expected SQLite connection.");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(connection);
        services.AddDbContext<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
        services.AddDbContextFactory<ApplicationDbContext>((sp, options) => options.UseSqlite(sp.GetRequiredService<SqliteConnection>()));
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
        services.AddScoped<BookingAvailabilityService>();
        var scope = services.BuildServiceProvider().CreateScope();
        return scope.ServiceProvider.GetRequiredService<BookingAvailabilityService>();
    }

    private static async Task<SqliteConnection> OpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);
        return connection;
    }

    private static async Task<ApplicationDbContext> CreateDbAsync()
    {
        var connection = await OpenConnectionAsync().ConfigureAwait(false);

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
