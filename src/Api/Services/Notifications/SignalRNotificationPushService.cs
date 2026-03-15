using Microsoft.AspNetCore.SignalR;
using Misa.Api.Middleware;
using Misa.Application.Abstractions.Notifications;

namespace Misa.Api.Services.Notifications;

public class SignalRNotificationPushService(
    IHubContext<EventHub> hub,
    ILogger<SignalRNotificationPushService> logger) : INotificationPushService
{
    public async Task NotifyUserChangedAsync(string userId)
    {
        try
        {
            await hub.Clients.User(userId).SendAsync("notifications-changed");
        }
        catch (Exception ex)
        {
            // Push failure is non-fatal — notifications are already persisted
            logger.LogWarning(ex, "Failed to push notifications-changed event via SignalR.");
        }
    }
}
