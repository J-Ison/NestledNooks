namespace NestledNooks.Services;

public interface IPriceLabsPricingSyncService
{
    Task SyncAllConfiguredPropertiesAsync(CancellationToken cancellationToken = default);
}
