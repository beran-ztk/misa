using System;
using System.Net.Http;
using System.Text.Json;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Events;
using Misa.Ui.Avalonia.Services.Realtime;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.Stores;

public partial class NavigationStore : ObservableObject
{
    public RealtimeEventsClient Realtime { get; } = new();

    public Action<Guid>? RefreshItemById { get; set; }
    public Action? RefreshCurrent { get; set; }

    public event Action<EventDto>? RealtimeEventReceived;

    private bool _realtimeWired;
    private Action<EventDto>? _realtimeHandler;

    public void WireRealtimeHandlers()
    {
        if (_realtimeWired)
            return;

        _realtimeWired = true;

        Console.WriteLine($"[NavStore] got event, store={GetHashCode()}");

        _realtimeHandler = evt =>
        {
            RealtimeEventReceived?.Invoke(evt);
            HandleGlobalRefresh(evt);
        };

        Realtime.EventReceived += _realtimeHandler;
    }

    public void UnwireRealtimeHandlers()
    {
        if (!_realtimeWired)
            return;

        if (_realtimeHandler is not null)
            Realtime.EventReceived -= _realtimeHandler;

        _realtimeHandler = null;
        _realtimeWired = false;
    }

    private void HandleGlobalRefresh(EventDto evt)
    {
        if (!string.Equals(evt.EventType, "DeadlineRemoved", StringComparison.Ordinal))
            return;

        Guid itemId;
        try
        {
            using var doc = JsonDocument.Parse(evt.Payload);
            itemId = doc.RootElement.GetProperty("itemId").GetGuid();
        }
        catch { return; }

        Dispatcher.UIThread.Post(() =>
        {
            RefreshItemById?.Invoke(itemId);
            RefreshCurrent?.Invoke();
        });
    }
}