using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Misa.Ui.Avalonia.App.Notifications;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Messaging;

public sealed class SignalRNotificationClient
{
    private readonly NotificationViewModel _notifications;
    private HubConnection? _connection;
    public SignalRNotificationClient(NotificationViewModel notifications)
    {
        _notifications = notifications;
    }

    public async Task StartAsync(string baseAddress)
    {
        if (_connection is not null)
            return;
        
        _connection = new HubConnectionBuilder()
            .WithUrl(baseAddress + "/hubs/updates")
            .WithAutomaticReconnect()
            .Build();
        
        _connection.On<string>("OutboxEvent", payload =>
        {
            _notifications.Publish(NotificationLevel.Info, payload);
        });
        
        await _connection.StartAsync();
        _notifications.Publish(NotificationLevel.Success, "SignalR Notification Client successfully started");
    }
}