using NestledNooks.Models;

namespace NestledNooks.Services;

public interface IStayPriceComparisonService
{
    Task<StayPriceComparison> GetComparisonAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int petCount = 0,
        CancellationToken cancellationToken = default);
}
