using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NestledNooks.Services;

namespace NestledNooks.Tests.Services;

public sealed class PriceLabsChannelListingResolverTests
{
    [Fact]
    public async Task ResolveAsync_UsesExplicitAirbnbListingId()
    {
        var resolver = CreateResolver(new FakePriceLabsApiClient());

        var property = new PropertyBookingOptions
        {
            Slug = "deerfield-retreat",
            DisplayName = "Deerfield Retreat",
            PriceLabsAirbnbListingId = "airbnb-123",
        };

        var result = await resolver.ResolveAsync(property, "airbnb");

        Assert.NotNull(result);
        Assert.Equal("airbnb-123", result.ListingId);
        Assert.Equal("airbnb", result.Pms);
    }

    [Fact]
    public async Task ResolveAsync_MatchesListingByPmsAndName()
    {
        var resolver = CreateResolver(new FakePriceLabsApiClient(
            new PriceLabsListingInfo { ListingId = "vrbo-999", Name = "Deerfield Retreat on Vrbo", Pms = "vrbo" },
            new PriceLabsListingInfo { ListingId = "airbnb-888", Name = "Deerfield Retreat", Pms = "airbnb" }));

        var property = new PropertyBookingOptions
        {
            Slug = "deerfield-retreat",
            DisplayName = "Deerfield Retreat",
        };

        var airbnb = await resolver.ResolveAsync(property, "airbnb");
        var vrbo = await resolver.ResolveAsync(property, "vrbo");

        Assert.Equal("airbnb-888", airbnb?.ListingId);
        Assert.Equal("vrbo-999", vrbo?.ListingId);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNullWhenChannelMissing()
    {
        var resolver = CreateResolver(new FakePriceLabsApiClient(
            new PriceLabsListingInfo { ListingId = "vrbo-only", Name = "Deerfield Retreat", Pms = "vrbo" }));

        var property = new PropertyBookingOptions
        {
            Slug = "deerfield-retreat",
            DisplayName = "Deerfield Retreat",
        };

        var airbnb = await resolver.ResolveAsync(property, "airbnb");

        Assert.Null(airbnb);
    }

    private static PriceLabsChannelListingResolver CreateResolver(IPriceLabsApiClient apiClient)
    {
        return new PriceLabsChannelListingResolver(
            apiClient,
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PriceLabsChannelListingResolver>.Instance);
    }

    private sealed class FakePriceLabsApiClient(params PriceLabsListingInfo[] listings) : IPriceLabsApiClient
    {
        public Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PriceLabsListingInfo>>(listings);

        public Task<IReadOnlyList<PriceLabsDayPrice>> GetListingPricesAsync(
            string listingId,
            string pms,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PriceLabsDayPrice>>([]);
    }
}
