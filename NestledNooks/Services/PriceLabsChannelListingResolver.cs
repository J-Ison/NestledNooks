using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace NestledNooks.Services;

public sealed record PriceLabsChannelReference(string ListingId, string Pms);

public interface IPriceLabsChannelListingResolver
{
    Task<PriceLabsChannelReference?> ResolveAsync(
        PropertyBookingOptions property,
        string channelPms,
        CancellationToken cancellationToken = default);
}

public sealed class PriceLabsChannelListingResolver(
    IPriceLabsApiClient apiClient,
    IMemoryCache cache,
    ILogger<PriceLabsChannelListingResolver> logger) : IPriceLabsChannelListingResolver
{
    private static readonly TimeSpan ListingsCacheDuration = TimeSpan.FromHours(6);

    public async Task<PriceLabsChannelReference?> ResolveAsync(
        PropertyBookingOptions property,
        string channelPms,
        CancellationToken cancellationToken = default)
    {
        var pms = channelPms.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(pms))
            return null;

        var explicitId = pms switch
        {
            "airbnb" => property.PriceLabsAirbnbListingId?.Trim(),
            "vrbo" => property.PriceLabsVrboListingId?.Trim(),
            _ => null,
        };

        if (!string.IsNullOrWhiteSpace(explicitId))
            return new PriceLabsChannelReference(explicitId, pms);

        var listings = await GetListingsAsync(cancellationToken).ConfigureAwait(false);
        var channelListings = listings
            .Where(l => string.Equals(l.Pms?.Trim(), pms, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (channelListings.Count == 0)
        {
            logger.LogDebug(
                "No PriceLabs listings found for PMS {Pms} ({Slug}).",
                pms,
                property.Slug);
            return null;
        }

        var configuredId = property.PriceLabsListingId?.Trim();
        if (!string.IsNullOrWhiteSpace(configuredId))
        {
            var configuredMatch = channelListings.FirstOrDefault(l =>
                string.Equals(l.ListingId.Trim(), configuredId, StringComparison.Ordinal));
            if (configuredMatch is not null)
                return new PriceLabsChannelReference(configuredMatch.ListingId.Trim(), pms);
        }

        var displayName = property.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var nameMatch = channelListings.FirstOrDefault(l => NameMatches(l.Name, displayName));
            if (nameMatch is not null)
                return new PriceLabsChannelReference(nameMatch.ListingId.Trim(), pms);
        }

        if (channelListings.Count == 1)
            return new PriceLabsChannelReference(channelListings[0].ListingId.Trim(), pms);

        logger.LogWarning(
            "Multiple PriceLabs {Pms} listings found for {Slug}; set PriceLabs{Channel}ListingId in config.",
            pms,
            property.Slug,
            pms.Equals("airbnb", StringComparison.Ordinal) ? "Airbnb" : "Vrbo");

        return null;
    }

    private async Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            "pricelabs:listings",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ListingsCacheDuration;
                return await apiClient.GetListingsAsync(cancellationToken).ConfigureAwait(false);
            }) ?? [];
    }

    private static bool NameMatches(string? listingName, string propertyDisplayName)
    {
        if (string.IsNullOrWhiteSpace(listingName))
            return false;

        var left = NormalizeName(listingName);
        var right = NormalizeName(propertyDisplayName);
        return left.Contains(right, StringComparison.Ordinal)
            || right.Contains(left, StringComparison.Ordinal);
    }

    private static string NormalizeName(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
}
