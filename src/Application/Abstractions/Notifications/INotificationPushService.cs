namespace Misa.Application.Abstractions.Notifications;

public interface INotificationPushService
{
    Task NotifyUserChangedAsync(string userId);
}
