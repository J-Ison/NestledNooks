using Microsoft.Extensions.Options;

namespace NestledNooks.Services;

public sealed class CalendarSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BookingOptions _options;
    private readonly ILogger<CalendarSyncHostedService> _logger;

    public CalendarSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<BookingOptions> options,
        ILogger<CalendarSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(30, _options.CalendarSyncIntervalMinutes));

        // Avoid competing with app startup; hosted service owns the schedule.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var availability = scope.ServiceProvider.GetRequiredService<IBookingAvailabilityService>();
                await availability.SyncExternalCalendarsAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled calendar sync failed.");
            }

            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
        }
    }
}
