using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Services;
using NestledNooks.Tests.Infrastructure;

namespace NestledNooks.Tests.Services;

public sealed class BookingRequestServiceDepositTests
{
    [Fact]
    public async Task ApproveWithDepositAsync_RejectsDepositBelowMinimumPercent()
    {
        var scope = await CreateScopeAsync();
        try
        {
            var booking = await SeedPendingBookingAsync(scope.Db, totalAmount: 1000m);

            var result = await scope.Service.ApproveWithDepositAsync(
                booking.Id,
                depositAmount: 400m,
                statusNote: null,
                siteBaseUrl: "https://example.test");

            Assert.False(result.Succeeded);
            Assert.Contains("50%", result.ErrorMessage, StringComparison.Ordinal);
        }
        finally
        {
            await scope.DisposeAsync();
        }
    }

    [Fact]
    public async Task ApproveWithDepositAsync_AcceptsMinimumDepositAndMarksNonRefundable()
    {
        var scope = await CreateScopeAsync();
        try
        {
            var booking = await SeedPendingBookingAsync(scope.Db, totalAmount: 1000m);

            var result = await scope.Service.ApproveWithDepositAsync(
                booking.Id,
                depositAmount: 500m,
                statusNote: null,
                siteBaseUrl: "https://example.test");

            Assert.True(result.Succeeded, result.ErrorMessage);
            Assert.NotNull(result.PaymentUrl);

            var updated = await scope.Db.BookingRequests.FindAsync(booking.Id);
            Assert.NotNull(updated);
            Assert.Equal(BookingStatuses.Approved, updated!.Status);
            Assert.Equal(500m, updated.RequiredDepositAmount);
            Assert.True(updated.DepositNonRefundable);
        }
        finally
        {
            await scope.DisposeAsync();
        }
    }

    [Fact]
    public async Task UpdateStatusAsync_BlocksBareApprovedTransition()
    {
        var scope = await CreateScopeAsync();
        try
        {
            var booking = await SeedPendingBookingAsync(scope.Db, totalAmount: 500m);

            var result = await scope.Service.UpdateStatusAsync(booking.Id, BookingStatuses.Approved, null);

            Assert.False(result.Succeeded);
            Assert.Contains("Approve & request payment", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await scope.DisposeAsync();
        }
    }

    private sealed class TestScope : IAsyncDisposable
    {
        public required ApplicationDbContext Db { get; init; }
        public required BookingRequestService Service { get; init; }
        private Microsoft.Data.Sqlite.SqliteConnection? _connection;

        public static TestScope Create(ApplicationDbContext db, BookingRequestService service, Microsoft.Data.Sqlite.SqliteConnection connection) =>
            new() { Db = db, Service = service, _connection = connection };

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            if (_connection is not null)
                await _connection.DisposeAsync();
        }
    }

    private static async Task<TestScope> CreateScopeAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var email = new FakeEmailService();
        var availability = new FakeAvailabilityService();
        var pricing = CreatePricingService(db);
        var stripe = new FakeStripePaymentService();
        var stripeOptions = Options.Create(new StripeOptions
        {
            Enabled = true,
            PublishableKey = "pk_test_x",
            SecretKey = "sk_test_x",
            DefaultMinimumDepositPercent = 50,
        });

        var service = new BookingRequestService(
            db,
            email,
            availability,
            pricing,
            stripe,
            new FakeGuestEmailWrapperService(),
            new FakeSiteSettingsService(),
            new Microsoft.AspNetCore.Http.HttpContextAccessor(),
            stripeOptions,
            NullLogger<BookingRequestService>.Instance);

        return TestScope.Create(db, service, connection);
    }

    private static BookingPricingService CreatePricingService(ApplicationDbContext db)
    {
        var bookingOptions = Options.Create(new BookingOptions
        {
            Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = "deerfield-retreat",
                    DisplayName = "Deerfield Retreat",
                    NightlyRate = 225,
                    CleaningFee = 150,
                    MaxGuests = 12,
                    MaxPets = 4,
                },
            ],
        });

        return new BookingPricingService(db, bookingOptions);
    }

    private static async Task<BookingRequest> SeedPendingBookingAsync(ApplicationDbContext db, decimal totalAmount)
    {
        var booking = new BookingRequest
        {
            BookingNumber = "NN-TEST-001",
            PropertySlug = "deerfield-retreat",
            GuestFullName = "Test Guest",
            GuestEmail = "guest@example.com",
            CheckIn = new DateOnly(2026, 7, 1),
            CheckOut = new DateOnly(2026, 7, 4),
            GuestCount = 2,
            PetCount = 0,
            NightCount = 3,
            NightlyRate = 200,
            CleaningFee = 150,
            PetFee = 0,
            Subtotal = totalAmount - 150,
            TotalAmount = totalAmount,
            Status = BookingStatuses.Pending,
            PaymentStatus = PaymentStatuses.Unpaid,
            CreatedAtUtc = DateTime.UtcNow,
            StatusUpdatedAtUtc = DateTime.UtcNow,
        };

        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();
        return booking;
    }

    private sealed class FakeSiteSettingsService : ISiteSettingsService
    {
        public bool DirectBookingsEnabled { get; set; } = true;

        public Task<SiteSettingsSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SiteSettingsSnapshot(DirectBookingsEnabled));

        public Task<SiteSettingsSaveResult> SetDirectBookingsEnabledAsync(
            bool enabled,
            CancellationToken cancellationToken = default)
        {
            DirectBookingsEnabled = enabled;
            return Task.FromResult(new SiteSettingsSaveResult(true, null));
        }
    }

    private sealed class FakeGuestEmailWrapperService : IGuestEmailWrapperService
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

    private sealed class FakeAvailabilityService : IBookingAvailabilityService
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

    private sealed class FakeStripePaymentService : IStripePaymentService
    {
        public bool IsConfigured => true;

        public StripeOptions Options { get; } = new()
        {
            Enabled = true,
            PublishableKey = "pk_test_x",
            SecretKey = "sk_test_x",
        };

        public Task<BookingPaymentLinkResult> CreatePaymentLinkAsync(
            BookingRequest booking,
            string purpose,
            decimal amount,
            string siteBaseUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BookingPaymentLinkResult(
                true,
                null,
                new BookingPaymentLink
                {
                    Id = 1,
                    BookingRequestId = booking.Id,
                    Token = "testtoken",
                    Purpose = purpose,
                    Amount = amount,
                    CreatedAtUtc = DateTime.UtcNow,
                },
                $"{siteBaseUrl}/pay/testtoken"));

        public Task<string?> GetCheckoutRedirectUrlAsync(
            string token,
            string siteBaseUrl,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>("https://checkout.stripe.test/session");

        public Task HandleWebhookAsync(
            string json,
            string stripeSignatureHeader,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StripeCheckoutConfirmResult> ConfirmCheckoutSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new StripeCheckoutConfirmResult(true, true, "NN-TEST-001", null));
    }
}
