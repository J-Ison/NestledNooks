namespace NestledNooks.Services;

public interface IBookingAvailabilityService
{
    Task<IReadOnlyList<DateOnly>> GetUnavailableDatesAsync(
        string propertySlug,
        DateOnly from,
        DateOnly to,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsRangeAvailableAsync(
        string propertySlug,
        DateOnly checkIn,
        DateOnly checkOut,
        int? excludeBookingId = null,
        CancellationToken cancellationToken = default);

    Task SyncExternalCalendarsAsync(bool force = false, CancellationToken cancellationToken = default);
}
