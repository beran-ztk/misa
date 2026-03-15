using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.Messaging;

public sealed class SignalRNotificationClient(UserState userState, string hubUrl)
{
    private HubConnection? _connection;

    internal event Func<Task>? NotificationsChanged;

    public async Task EnsureConnectedAsync()
    {
        if (_connection is not null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(userState.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On("notifications-changed", () =>
        {
            var handler = NotificationsChanged;
            if (handler is not null)
                _ = handler();
        });

        try
        {
            await _connection.StartAsync();
        }
        catch (Exception)
        {
            // Connection failure on startup is non-fatal — manual refresh still works.
            // WithAutomaticReconnect will retry once the server becomes available.
        }
    }
}
