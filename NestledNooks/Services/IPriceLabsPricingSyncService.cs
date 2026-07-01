namespace NestledNooks.Services;

public interface IPriceLabsPricingSyncService
{
    Task SyncAllConfiguredPropertiesAsync(bool force = false, CancellationToken cancellationToken = default);
}
