namespace NestledNooks.Services;

public sealed class PriceLabsListingInfo
{
    public required string ListingId { get; init; }
    public string? Name { get; init; }
    public string? Pms { get; init; }
}

public sealed class PriceLabsDayPrice
{
    public required DateOnly Date { get; init; }
    public required decimal Rate { get; init; }
    public int? MinimumStay { get; init; }
}

public interface IPriceLabsApiClient
{
    Task<IReadOnlyList<PriceLabsListingInfo>> GetListingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriceLabsDayPrice>> GetListingPricesAsync(
        string listingId,
        string pms,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
