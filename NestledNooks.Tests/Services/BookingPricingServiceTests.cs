using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NestledNooks.Data;
using NestledNooks.Models;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class BookingPricingServiceTests
{
    [Fact]
    public async Task CalculateAsync_UsesFlatRateWhenNoSyncedPrices()
    {
        await using var scope = await CreateScopeAsync();

        var quote = await scope.Service.CalculateAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4),
            petCount: 0);

        Assert.False(quote.UsesDynamicPricing);
        Assert.Equal(3, quote.Nights);
        Assert.Equal(675m, quote.Subtotal);
        Assert.Equal(875m, quote.TotalAmount);
    }

    [Fact]
    public async Task CalculateAsync_SumsSyncedNightlyRates()
    {
        await using var scope = await CreateScopeAsync();

        scope.Db.PropertyNightlyRates.AddRange(
            new PropertyNightlyRate { PropertySlug = PropertySeedData.DeerfieldSlug, Date = new DateOnly(2026, 8, 1), Rate = 300m, UpdatedAtUtc = DateTime.UtcNow },
            new PropertyNightlyRate { PropertySlug = PropertySeedData.DeerfieldSlug, Date = new DateOnly(2026, 8, 2), Rate = 350m, UpdatedAtUtc = DateTime.UtcNow },
            new PropertyNightlyRate { PropertySlug = PropertySeedData.DeerfieldSlug, Date = new DateOnly(2026, 8, 3), Rate = 400m, UpdatedAtUtc = DateTime.UtcNow });

        await scope.Db.SaveChangesAsync();

        var quote = await scope.Service.CalculateAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 4),
            petCount: 1);

        Assert.True(quote.UsesDynamicPricing);
        Assert.Equal(1050m, quote.Subtotal);
        Assert.Equal(350m, quote.NightlyRate);
        Assert.Equal(50m, quote.PetFee);
        Assert.Equal(1300m, quote.TotalAmount);
    }

    [Fact]
    public async Task CalculateAsync_UsesPropertyMinimumStay_NotPriceLabsCheckInMinimum()
    {
        await using var scope = await CreateScopeAsync(minimumNights: 2);

        scope.Db.PropertyNightlyRates.Add(new PropertyNightlyRate
        {
            PropertySlug = PropertySeedData.DeerfieldSlug,
            Date = new DateOnly(2026, 9, 10),
            Rate = 250m,
            MinimumStay = 4,
            UpdatedAtUtc = DateTime.UtcNow,
        });

        await scope.Db.SaveChangesAsync();

        var quote = await scope.Service.CalculateAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 9, 10),
            new DateOnly(2026, 9, 12),
            petCount: 0);

        Assert.Equal(2, quote.Nights);
    }

    [Fact]
    public async Task CalculateAsync_AppliesWeeklyDiscount()
    {
        await using var scope = await CreateScopeAsync();

        var property = await scope.Db.RentalProperties.SingleAsync(p => p.Slug == PropertySeedData.DeerfieldSlug);
        property.DiscountsJson = PropertyDiscountsJson.Serialize(PropertyBookingDiscounts.Defaults());
        await scope.Db.SaveChangesAsync();

        var quote = await scope.Service.CalculateAsync(
            PropertySeedData.DeerfieldSlug,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 8),
            petCount: 0);

        Assert.Equal(7, quote.Nights);
        Assert.Equal(1417.50m, quote.Subtotal);
        Assert.Equal(157.50m, quote.DiscountAmount);
        Assert.Equal("Weekly discount", quote.DiscountLabel);
        Assert.Equal(1617.50m, quote.TotalAmount);
    }

    private static async Task<PricingTestScope> CreateScopeAsync(int minimumNights = 2)
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
                    PetFeePerStay = 75m,
                    MinimumNights = minimumNights,
                },
            ];
        });
        services.AddScoped<BookingPricingService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var listing = PropertySeedData.CreateDeerfieldRetreat();
        listing.MinimumNights = minimumNights;
        listing.MinAdvanceBookingDays = 0;
        listing.MaxBookingDaysAhead = 730;
        listing.CleaningFee = ListingSettingsDefaults.CleaningFee;
        db.RentalProperties.Add(listing);
        await db.SaveChangesAsync().ConfigureAwait(false);

        return new PricingTestScope(provider, scope, db, connection);
    }

    private sealed class PricingTestScope : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;
        private readonly SqliteConnection _connection;

        public PricingTestScope(
            ServiceProvider provider,
            IServiceScope scope,
            ApplicationDbContext db,
            SqliteConnection connection)
        {
            _provider = provider;
            _scope = scope;
            Db = db;
            _connection = connection;
            Service = scope.ServiceProvider.GetRequiredService<BookingPricingService>();
        }

        public ApplicationDbContext Db { get; }

        public BookingPricingService Service { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
