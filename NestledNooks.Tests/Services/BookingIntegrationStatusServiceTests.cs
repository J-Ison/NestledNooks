using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class BookingIntegrationStatusServiceTests
{
    [Fact]
    public async Task GetSnapshotAsync_ReportsAirbnbSyncedWhenBlocksExist()
    {
        await using var scope = await CreateScopeAsync(airbnbUrl: "https://example.test/airbnb.ics");

        scope.Db.ExternalCalendarEvents.Add(new ExternalCalendarEvent
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            Source = "Airbnb",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 5),
            SyncedAtUtc = DateTime.UtcNow,
        });

        await scope.Db.SaveChangesAsync();

        var snapshot = await scope.Service.GetSnapshotAsync(PropertySeedData.DeerfieldSlug);

        Assert.True(snapshot.Airbnb.IsConfigured);
        Assert.True(snapshot.Airbnb.IsSynced);
        Assert.Equal(1, snapshot.Airbnb.ItemCount);
        Assert.False(snapshot.Vrbo.IsConfigured);
        Assert.False(snapshot.PriceLabs.IsSynced);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReportsPriceLabsNotSyncedWithListingHint()
    {
        await using var scope = await CreateScopeAsync(
            priceLabsEnabled: true,
            priceLabsListingId: "1385672541118250169");

        var snapshot = await scope.Service.GetSnapshotAsync(PropertySeedData.DeerfieldSlug);

        Assert.True(snapshot.PriceLabs.IsConfigured);
        Assert.False(snapshot.PriceLabs.IsSynced);
        Assert.Contains("1385672541118250169"[^6..], snapshot.PriceLabs.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSnapshotAsync_ReportsPriceLabsSyncedWhenRatesExist()
    {
        await using var scope = await CreateScopeAsync(
            priceLabsEnabled: true,
            priceLabsListingId: "listing-1");

        scope.Db.PropertyNightlyRates.Add(new PropertyNightlyRate
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            Date = new DateOnly(2026, 9, 1),
            Rate = 250m,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        await scope.Db.SaveChangesAsync();

        var snapshot = await scope.Service.GetSnapshotAsync(PropertySeedData.DeerfieldSlug);

        Assert.True(snapshot.PriceLabs.IsConfigured);
        Assert.True(snapshot.PriceLabs.IsSynced);
        Assert.Equal(1, snapshot.PriceLabs.ItemCount);
    }

    private static async Task<IntegrationStatusTestScope> CreateScopeAsync(
        string? airbnbUrl = null,
        bool priceLabsEnabled = false,
        string? priceLabsListingId = null)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.Configure<BookingOptions>(opts =>
        {
            opts.Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = PropertySeedData.DeerfieldSlug,
                    DisplayName = "Deerfield Retreat",
                    AirbnbIcalUrl = airbnbUrl,
                    PriceLabsListingId = priceLabsListingId,
                },
            ];
        });
        services.Configure<PriceLabsOptions>(opts =>
        {
            opts.Enabled = priceLabsEnabled;
            opts.ApiKey = priceLabsEnabled ? "test-key" : "";
        });
        services.AddScoped<IBookingIntegrationStatusService, BookingIntegrationStatusService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return new IntegrationStatusTestScope(provider, scope, db, connection);
    }

    private sealed class IntegrationStatusTestScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _connection;

        public IntegrationStatusTestScope(
            ServiceProvider provider,
            IServiceScope scope,
            ApplicationDbContext db,
            SqliteConnection connection)
        {
            _provider = provider;
            _scope = scope;
            Db = db;
            _connection = connection;
            Service = scope.ServiceProvider.GetRequiredService<IBookingIntegrationStatusService>();
        }

        public ApplicationDbContext Db { get; }

        public IBookingIntegrationStatusService Service { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
