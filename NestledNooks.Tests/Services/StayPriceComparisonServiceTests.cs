using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class StayPriceComparisonServiceTests
{
    [Fact]
    public async Task GetComparisonAsync_ResolvesSeparateChannelListingsAndIncludesGuestFees()
    {
        await using var scope = await CreateScopeAsync();
        var apiClient = new FakePriceLabsApiClient();
        var resolver = new FakeChannelResolver(
            new PriceLabsChannelReference("airbnb-1", "airbnb"),
            new PriceLabsChannelReference("vrbo-2", "vrbo"));
        var service = new StayPriceComparisonService(
            scope.Pricing,
            apiClient,
            resolver,
            Options.Create(new PriceLabsOptions
            {
                Enabled = true,
                ApiKey = "test",
                DefaultAirbnbGuestServiceFeePercent = 10m,
                DefaultVrboGuestServiceFeePercent = 8m,
            }),
            NullLogger<StayPriceComparisonService>.Instance);

        var comparison = await service.GetComparisonAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.NotNull(comparison.Airbnb.TotalAmount);
        Assert.NotNull(comparison.Vrbo.TotalAmount);

        // 312 + 318 lodging + 200 cleaning = 830; +10% guest fee = 913
        Assert.Equal(913m, comparison.Airbnb.TotalAmount);
        Assert.Equal(896.40m, comparison.Vrbo.TotalAmount);
        Assert.Equal("airbnb-1", apiClient.LastAirbnbListingId);
        Assert.Equal("vrbo-2", apiClient.LastVrboListingId);
    }

    [Fact]
    public async Task GetComparisonAsync_AirbnbUnavailableWhenListingNotResolved()
    {
        await using var scope = await CreateScopeAsync();
        var resolver = new FakeChannelResolver(
            airbnb: null,
            vrbo: new PriceLabsChannelReference("vrbo-2", "vrbo"));
        var service = new StayPriceComparisonService(
            scope.Pricing,
            new FakePriceLabsApiClient(),
            resolver,
            Options.Create(new PriceLabsOptions { Enabled = true, ApiKey = "test" }),
            NullLogger<StayPriceComparisonService>.Instance);

        var comparison = await service.GetComparisonAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 6, 24),
            new DateOnly(2026, 6, 26));

        Assert.Null(comparison.Airbnb.TotalAmount);
        Assert.Contains("Airbnb", comparison.Airbnb.Note ?? "", StringComparison.Ordinal);
        Assert.NotNull(comparison.Vrbo.TotalAmount);
    }

    private static async Task<ComparisonTestScope> CreateScopeAsync()
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
                    NightlyRate = 225m,
                    CleaningFee = 150m,
                    MinimumNights = 2,
                },
            ];
        });
        services.AddScoped<BookingPricingService>();

        var provider = services.BuildServiceProvider();
        var serviceScope = provider.CreateScope();
        var db = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var listing = PropertySeedData.CreateDeerfieldRetreat();
        listing.MinimumNights = 2;
        listing.MinAdvanceBookingDays = 0;
        listing.MaxBookingDaysAhead = 730;
        listing.CleaningFee = ListingSettingsDefaults.CleaningFee;
        db.RentalProperties.Add(listing);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return new ComparisonTestScope(
            provider,
            serviceScope,
            db,
            connection,
            serviceScope.ServiceProvider.GetRequiredService<BookingPricingService>());
    }

    private sealed class ComparisonTestScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _connection;

        public ComparisonTestScope(
            ServiceProvider provider,
            IServiceScope scope,
            ApplicationDbContext db,
            SqliteConnection connection,
            BookingPricingService pricing)
        {
            _provider = provider;
            _scope = scope;
            Db = db;
            _connection = connection;
            Pricing = pricing;
        }

        public ApplicationDbContext Db { get; }
        public BookingPricingService Pricing { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class FakeChannelResolver(
        PriceLabsChannelReference? airbnb,
        PriceLabsChannelReference? vrbo) : IPriceLabsChannelListingResolver
    {
        public Task<PriceLabsChannelReference?> ResolveAsync(
            PropertyBookingOptions property,
            string channelPms,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(channelPms switch
            {
                "airbnb" => airbnb,
                "vrbo" => vrbo,
                _ => null,
            });
    }

    private sealed class FakePriceLabsApiClient : IPriceLabsApiClient
    {
        public string? LastAirbnbListingId { get; private set; }
        public string? LastVrboListingId { get; private set; }

        public Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PriceLabsListingInfo>>([]);

        public Task<IReadOnlyList<PriceLabsDayPrice>> GetListingPricesAsync(
            string listingId,
            string pms,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            if (pms == "airbnb")
                LastAirbnbListingId = listingId;
            if (pms == "vrbo")
                LastVrboListingId = listingId;

            return Task.FromResult<IReadOnlyList<PriceLabsDayPrice>>(
            [
                new PriceLabsDayPrice { Date = startDate, Rate = 312m },
                new PriceLabsDayPrice { Date = startDate.AddDays(1), Rate = 318m },
            ]);
        }
    }
}
