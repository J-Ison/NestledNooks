using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class PriceLabsPricingSyncServiceTests
{
    [Fact]
    public async Task SyncAllConfiguredPropertiesAsync_UpsertsRatesFromApi()
    {
        await using var scope = await CreateScopeAsync(new FakePriceLabsApiClient());

        await scope.Service.SyncAllConfiguredPropertiesAsync();

        var rates = await scope.Db.PropertyNightlyRates
            .Where(r => r.PropertySlug == PropertySeedData.DeerfieldSlug)
            .OrderBy(r => r.Date)
            .ToListAsync();

        Assert.Equal(2, rates.Count);
        Assert.Equal(300m, rates[0].Rate);
        Assert.Equal(3, rates[0].MinimumStay);
        Assert.Equal(325m, rates[1].Rate);
    }

    [Fact]
    public async Task SyncAllConfiguredPropertiesAsync_SkipsWhenDisabled()
    {
        var client = new FakePriceLabsApiClient();
        await using var scope = await CreateScopeAsync(client, enabled: false);

        await scope.Service.SyncAllConfiguredPropertiesAsync();

        Assert.Empty(await scope.Db.PropertyNightlyRates.ToListAsync());
        Assert.Equal(0, client.PriceCalls);
    }

    private static async Task<SyncTestScope> CreateScopeAsync(
        IPriceLabsApiClient apiClient,
        bool enabled = true)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));
        services.AddSingleton(apiClient);
        services.Configure<BookingOptions>(opts =>
        {
            opts.Properties =
            [
                new PropertyBookingOptions
                {
                    Slug = PropertySeedData.DeerfieldSlug,
                    DisplayName = "Deerfield Retreat",
                    PriceLabsListingId = "listing-123",
                    PriceLabsPms = "airbnb",
                },
            ];
        });
        services.Configure<PriceLabsOptions>(opts =>
        {
            opts.Enabled = enabled;
            opts.ApiKey = "test-key";
            opts.SyncDaysAhead = 30;
        });
        services.AddScoped<IPriceLabsPricingSyncService, PriceLabsPricingSyncService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        return new SyncTestScope(provider, scope, db, connection);
    }

    private sealed class FakePriceLabsApiClient : IPriceLabsApiClient
    {
        public int PriceCalls { get; private set; }

        public Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PriceLabsListingInfo>>([]);

        public Task<IReadOnlyList<PriceLabsDayPrice>> GetListingPricesAsync(
            string listingId,
            string pms,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            PriceCalls++;
            return Task.FromResult<IReadOnlyList<PriceLabsDayPrice>>(
            [
                new PriceLabsDayPrice { Date = startDate, Rate = 300m, MinimumStay = 3 },
                new PriceLabsDayPrice { Date = startDate.AddDays(1), Rate = 325m, MinimumStay = 3 },
            ]);
        }
    }

    private sealed class SyncTestScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _connection;

        public SyncTestScope(
            ServiceProvider provider,
            IServiceScope scope,
            ApplicationDbContext db,
            SqliteConnection connection)
        {
            _provider = provider;
            _scope = scope;
            Db = db;
            _connection = connection;
            Service = scope.ServiceProvider.GetRequiredService<IPriceLabsPricingSyncService>();
        }

        public ApplicationDbContext Db { get; }

        public IPriceLabsPricingSyncService Service { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
