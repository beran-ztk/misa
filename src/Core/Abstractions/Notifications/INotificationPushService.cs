namespace Misa.Core.Abstractions.Notifications;

public interface INotificationPushService
{
    Task NotifyUserChangedAsync(string userId);
}
