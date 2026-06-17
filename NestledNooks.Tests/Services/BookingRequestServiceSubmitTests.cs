using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Models;
using NestledNooks.Services;
using NestledNooks.Tests.Infrastructure;

namespace NestledNooks.Tests.Services;

public sealed class BookingRequestServiceSubmitTests
{
    [Fact]
    public async Task SubmitAsync_SavesBookingWhenCircuitContextHasPendingNightlyRates()
    {
        await using var scope = await CreateScopeAsync();
        var checkIn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);
        var checkOut = checkIn.AddDays(3);

        scope.Db.PropertyNightlyRates.Add(new PropertyNightlyRate
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            Date = checkIn,
            Rate = 299m,
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await scope.Db.SaveChangesAsync();

        scope.Db.PropertyNightlyRates.Add(new PropertyNightlyRate
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            Date = checkIn.AddDays(1),
            Rate = 299m,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        var result = await scope.Service.SubmitAsync(new BookingFormModel
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            GuestFullName = "Jordan Guest",
            GuestEmail = "guest@example.com",
            CheckIn = checkIn,
            CheckOut = checkOut,
            GuestCount = 2,
            PetCount = 0,
        }, userId: null);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.BookingId);

        var saved = await scope.Db.BookingRequests.FindAsync(result.BookingId);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatuses.Pending, saved!.Status);
        Assert.StartsWith("NN-", saved.BookingNumber, StringComparison.Ordinal);
    }

    private sealed class SubmitTestScope(ApplicationDbContext db, BookingRequestService service) : IAsyncDisposable
    {
        public ApplicationDbContext Db { get; } = db;
        public BookingRequestService Service { get; } = service;
        private Microsoft.Data.Sqlite.SqliteConnection? _connection;

        public static SubmitTestScope Create(
            ApplicationDbContext db,
            BookingRequestService service,
            Microsoft.Data.Sqlite.SqliteConnection connection) =>
            new(db, service) { _connection = connection };

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }

    private static async Task<SubmitTestScope> CreateScopeAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.RentalProperties.Add(PropertySeedData.CreateDeerfieldRetreat());
        await db.SaveChangesAsync();

        var dbFactory = new TestDbContextFactory(options);
        var bookingOptions = Options.Create(new BookingOptions
        {
            Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = PropertySeedData.DeerfieldSlug,
                    DisplayName = "Deerfield Retreat",
                    NightlyRate = 225,
                    CleaningFee = 200,
                    MaxGuests = 12,
                    MaxPets = 4,
                },
            ],
        });

        var service = new BookingRequestService(
            db,
            dbFactory,
            new FakeEmailService(),
            new AlwaysAvailableService(),
            new BookingPricingService(db, bookingOptions),
            new NoOpStripePaymentService(),
            new NoOpGuestEmailWrapperService(),
            new EnabledSiteSettingsService(),
            new Microsoft.AspNetCore.Http.HttpContextAccessor(),
            Options.Create(new StripeOptions()),
            NullLogger<BookingRequestService>.Instance);

        return SubmitTestScope.Create(db, service, connection);
    }

    private sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
        : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }

    private sealed class AlwaysAvailableService : IBookingAvailabilityService
    {
        public Task<bool> IsRangeAvailableAsync(
            string propertySlug,
            DateOnly checkIn,
            DateOnly checkOut,
            int? excludeBookingId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task SyncExternalCalendarsAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<DateOnly>> GetUnavailableDatesAsync(
            string propertySlug,
            DateOnly from,
            DateOnly to,
            int? excludeBookingId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DateOnly>>([]);
    }

    private sealed class EnabledSiteSettingsService : ISiteSettingsService
    {
        public Task<SiteSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SiteSettingsSnapshot.Defaults() with { DirectBookingsEnabled = true });

        public Task<SiteSettingsSaveResult> SetDirectBookingsEnabledAsync(
            bool enabled,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new SiteSettingsSaveResult(true, null));
    }

    private sealed class NoOpGuestEmailWrapperService : IGuestEmailWrapperService
    {
        public Task<GuestEmailWrapperSettings> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new GuestEmailWrapperSettings(
                GuestEmailWrapperDefaults.Header,
                GuestEmailWrapperDefaults.Footer));

        public Task<GuestEmailWrapperSaveResult> SaveAsync(
            string headerTemplate,
            string footerTemplate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GuestEmailWrapperSaveResult(true, null));

        public Task<string> ComposeFullBodyAsync(
            string guestMessage,
            BookingGuestMessageEmailPayload payload,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(guestMessage);

        public string ComposeFullBody(
            string guestMessage,
            BookingGuestMessageEmailPayload payload,
            GuestEmailWrapperSettings settings) =>
            guestMessage;
    }

    private sealed class NoOpStripePaymentService : IStripePaymentService
    {
        public bool IsConfigured => false;
        public StripeOptions Options { get; } = new();

        public Task<BookingPaymentLinkResult> CreatePaymentLinkAsync(
            BookingRequest booking,
            string purpose,
            decimal amount,
            string siteBaseUrl,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string?> GetCheckoutRedirectUrlAsync(
            string token,
            string siteBaseUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);

        public Task HandleWebhookAsync(
            string json,
            string stripeSignatureHeader,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StripeCheckoutConfirmResult> ConfirmCheckoutSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StripeCheckoutConfirmResult(false, false, null, null));
    }
}
