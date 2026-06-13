using Microsoft.Extensions.Options;

namespace NestledNooks.Services;

public sealed class PriceLabsPricingSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PriceLabsOptions _options;
    private readonly ILogger<PriceLabsPricingSyncHostedService> _logger;

    public PriceLabsPricingSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<PriceLabsOptions> options,
        ILogger<PriceLabsPricingSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("PriceLabs pricing sync is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(30, _options.SyncIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var sync = scope.ServiceProvider.GetRequiredService<IPriceLabsPricingSyncService>();
                await sync.SyncAllConfiguredPropertiesAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Scheduled PriceLabs pricing sync failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
