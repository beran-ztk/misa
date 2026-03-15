using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Messaging;
using Misa.Contract.Notifications;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Messaging;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Utilities.Notifications;

public enum NotificationFilter { All, Unread }

public sealed partial class NotificationItem : ObservableObject
{
    public NotificationItem(NotificationDto notification)
    {
        Id        = Guid.NewGuid();
        Title     = notification.Payload;
        Message   = string.Empty;
        Timestamp = notification.Timestamp;
        _isRead   = false;
    }

    public NotificationItem(NotificationEntryDto dto)
    {
        Id        = dto.Id;
        Title     = dto.Title;
        Message   = dto.Message;
        Timestamp = dto.CreatedAtUtc;
        _isRead   = dto.ReadAtUtc.HasValue;
    }

    public Guid           Id                 { get; }
    public string         Title              { get; }
    public string         Message            { get; }
    public bool           HasMessage         => !string.IsNullOrWhiteSpace(Message);
    public DateTimeOffset Timestamp          { get; }
    public string         TimestampFormatted => Timestamp.ToLocalTime().ToString("dd MMM · HH:mm", CultureInfo.InvariantCulture);

    [ObservableProperty] private bool _isRead;
    [ObservableProperty] private bool _isPendingDismiss;
}

public sealed partial class NotificationViewModel : ViewModelBase
{
    private const int PageSize = 25;

    private readonly NotificationGateway           _gateway;
    private readonly SignalRNotificationClient     _signalR;
    private readonly LayerProxy                   _layerProxy;
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _pendingDismiss = new();

    private DateTimeOffset? _oldestTimestamp;

    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public bool IsEmpty => Notifications.Count == 0;

    [ObservableProperty] private bool   _hasMore;
    [ObservableProperty] private int    _unreadCount;
    [ObservableProperty] private NotificationFilter _activeFilter = NotificationFilter.All;

    public bool IsFilterAll    => ActiveFilter == NotificationFilter.All;
    public bool IsFilterUnread => ActiveFilter == NotificationFilter.Unread;

    public bool HasUnread => UnreadCount > 0;

    public NotificationViewModel(NotificationGateway gateway, SignalRNotificationClient signalR, LayerProxy layerProxy)
    {
        _gateway    = gateway;
        _signalR    = signalR;
        _layerProxy = layerProxy;
        _signalR.NotificationsChanged += RefreshFromRemoteChangeAsync;
        Notifications.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        _ = LoadAsync();
    }

    partial void OnActiveFilterChanged(NotificationFilter value)
    {
        OnPropertyChanged(nameof(IsFilterAll));
        OnPropertyChanged(nameof(IsFilterUnread));
        _ = LoadAsync();
    }

    partial void OnUnreadCountChanged(int value) => OnPropertyChanged(nameof(HasUnread));

    public void Publish(NotificationDto notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, new NotificationItem(notification));
            UnreadCount++;
        });
    }

    // Full reload — clears the list and fetches the first page + global unread count.
    public async Task LoadAsync(CancellationToken ct = default)
    {
        _ = _signalR.EnsureConnectedAsync();

        var onlyUnread = ActiveFilter == NotificationFilter.Unread;

        var pageTask  = _gateway.GetPageAsync(limit: PageSize, onlyUnread: onlyUnread, ct: ct);
        var countTask = _gateway.GetUnreadCountAsync(ct);

        await Task.WhenAll(pageTask, countTask);

        var dtos  = pageTask.Result ?? [];
        var count = countTask.Result;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Notifications.Clear();
            foreach (var dto in dtos)
                Notifications.Add(new NotificationItem(dto));

            ApplyPagingState(dtos);
            UnreadCount = count;
        });
    }

    public async Task InitializeAsync() => await LoadAsync();

    // Smart refresh from a SignalR push — prepends new items only, updates unread count, shows toast.
    private async Task RefreshFromRemoteChangeAsync()
    {
        var onlyUnread = ActiveFilter == NotificationFilter.Unread;

        var pageTask  = _gateway.GetPageAsync(limit: PageSize, onlyUnread: onlyUnread, ct: CancellationToken.None);
        var countTask = _gateway.GetUnreadCountAsync(CancellationToken.None);

        await Task.WhenAll(pageTask, countTask);

        var dtos  = pageTask.Result ?? [];
        var count = countTask.Result;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existingIds = Notifications.Select(n => n.Id).ToHashSet();

            var insertIndex = 0;
            foreach (var dto in dtos)
            {
                if (!existingIds.Contains(dto.Id))
                {
                    var item = new NotificationItem(dto);
                    Notifications.Insert(insertIndex++, item);
                    _layerProxy.ShowToast(item.Title, item.HasMessage ? item.Message : null);
                }
            }

            UnreadCount = count;
        });
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void SetFilter(NotificationFilter filter)
    {
        if (ActiveFilter == filter) return;
        ActiveFilter = filter;
        // OnActiveFilterChanged triggers LoadAsync
    }

    // Appends the next page of older notifications.
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (_oldestTimestamp is null) return;

        var onlyUnread = ActiveFilter == NotificationFilter.Unread;
        var dtos = await _gateway.GetPageAsync(
            limit: PageSize,
            before: _oldestTimestamp,
            onlyUnread: onlyUnread,
            ct: CancellationToken.None) ?? [];

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var dto in dtos)
                Notifications.Add(new NotificationItem(dto));

            ApplyPagingState(dtos);
        });
    }

    private void ApplyPagingState(List<NotificationEntryDto> dtos)
    {
        if (dtos.Count > 0)
            _oldestTimestamp = dtos[^1].CreatedAtUtc;

        HasMore = dtos.Count == PageSize;
    }

    // Dismiss with 4-second undo window — backend is not called until the window expires.
    [RelayCommand]
    private async Task DismissAsync(Guid id)
    {
        var item = Notifications.FirstOrDefault(x => x.Id == id);
        if (item is null || item.IsPendingDismiss) return;

        item.IsPendingDismiss = true;

        var cts = new CancellationTokenSource();
        _pendingDismiss[id] = cts;

        try
        {
            await Task.Delay(4000, cts.Token);
        }
        catch (OperationCanceledException)
        {
            _pendingDismiss.TryRemove(id, out _);
            await Dispatcher.UIThread.InvokeAsync(() => item.IsPendingDismiss = false);
            return;
        }

        _pendingDismiss.TryRemove(id, out _);

        var ok = await _gateway.DismissAsync(id);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (ok)
            {
                Notifications.Remove(item);
                if (!item.IsRead)
                    UnreadCount = Math.Max(0, UnreadCount - 1);
            }
            else
            {
                item.IsPendingDismiss = false;
            }
        });
    }

    // Cancel a pending dismiss before the backend is called.
    [RelayCommand]
    private void UndoDismiss(Guid id)
    {
        if (_pendingDismiss.TryRemove(id, out var cts))
            cts.Cancel();
    }

    [RelayCommand]
    private async Task MarkAsReadAsync(Guid id)
    {
        var item = Notifications.FirstOrDefault(x => x.Id == id);
        if (item is null || item.IsRead || item.IsPendingDismiss) return;

        item.IsRead = true;
        UnreadCount = Math.Max(0, UnreadCount - 1);

        var ok = await _gateway.MarkAsReadAsync(id);
        if (!ok)
        {
            item.IsRead = false;
            UnreadCount++;
        }
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        var unread = Notifications.Where(x => !x.IsRead && !x.IsPendingDismiss).ToList();
        foreach (var item in unread)
            item.IsRead = true;

        var ok = await _gateway.MarkAllAsReadAsync();
        if (ok)
        {
            UnreadCount = 0;
        }
        else
        {
            foreach (var item in unread)
                item.IsRead = false;
        }
    }
}
