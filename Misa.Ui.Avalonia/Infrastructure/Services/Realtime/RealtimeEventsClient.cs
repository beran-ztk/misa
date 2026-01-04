using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Misa.Contract.Events;
using Misa.Ui.Avalonia.Stores;

namespace Misa.Ui.Avalonia.Services.Realtime;

public sealed class RealtimeEventsClient : IAsyncDisposable
{
    private HubConnection? _conn;

    public event Action<EventDto>? EventReceived;

    public async Task StartAsync(string baseUrl, CancellationToken ct = default)
    {
        if (_conn is not null)
            return;

        _conn = new HubConnectionBuilder()
            .WithUrl($"{baseUrl.TrimEnd('/')}/hubs/events")
            .WithAutomaticReconnect()
            .Build();

        _conn.On<EventDto>("event", evt =>
        {
            Console.WriteLine($"[SignalR] received {evt.EventType} {evt.Payload}");
            EventReceived?.Invoke(evt);
        });

        await _conn.StartAsync(ct);
        Console.WriteLine("[SignalR] Connected");
    }

    public async ValueTask DisposeAsync()
    {
        if (_conn is null) return;
        await _conn.DisposeAsync();
        _conn = null;
    }
}