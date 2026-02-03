using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Misa.Contract.Features.Messaging;
using Misa.Ui.Avalonia.App.Notifications;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Messaging;

public sealed class SignalRNotificationClient(NotificationViewModel notifications)
{
    private HubConnection? _connection;

    public async Task StartAsync(string baseAddress)
    {
        if (_connection is not null)
            return;
        
        _connection = new HubConnectionBuilder()
            .WithUrl(baseAddress + "/hubs/updates")
            .WithAutomaticReconnect()
            .Build();
        
        _connection.On<NotificationDto>(nameof(PublisherDto.Scheduler), notifications.Publish);
        
        await _connection.StartAsync();
    }
}