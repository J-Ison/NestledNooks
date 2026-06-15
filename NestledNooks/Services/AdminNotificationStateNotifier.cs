namespace NestledNooks.Services;

public sealed class AdminNotificationStateNotifier
{
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}
