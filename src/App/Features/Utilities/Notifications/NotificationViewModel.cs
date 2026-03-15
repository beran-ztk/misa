using System;
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

namespace Misa.Ui.Avalonia.Features.Utilities.Notifications;

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
}

public sealed partial class NotificationViewModel : ViewModelBase
{
    private const int PageSize = 25;

    private readonly NotificationGateway _gateway;

    private DateTimeOffset? _oldestTimestamp;

    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public bool IsEmpty => Notifications.Count == 0;

    [ObservableProperty] private bool _hasMore;

    public NotificationViewModel(NotificationGateway gateway)
    {
        _gateway = gateway;
        Notifications.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        _ = LoadAsync();
    }

    public void Publish(NotificationDto notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, new NotificationItem(notification));
        });
    }

    // Full reload — clears the list and fetches the first page.
    public async Task LoadAsync(CancellationToken ct = default)
    {
        var dtos = await _gateway.GetPageAsync(limit: PageSize, ct: ct) ?? [];

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Notifications.Clear();
            foreach (var dto in dtos)
                Notifications.Add(new NotificationItem(dto));

            ApplyPagingState(dtos);
        });
    }

    public async Task InitializeAsync() => await LoadAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    // Appends the next page of older notifications.
    [RelayCommand]
    private async Task LoadMoreAsync()
    {
        if (_oldestTimestamp is null) return;

        var dtos = await _gateway.GetPageAsync(limit: PageSize, before: _oldestTimestamp, ct: CancellationToken.None) ?? [];

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

    [RelayCommand]
    private async Task DismissAsync(Guid id)
    {
        var ok = await _gateway.DismissAsync(id);
        if (!ok) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var item = Notifications.FirstOrDefault(x => x.Id == id);
            if (item != null)
                Notifications.Remove(item);
        });
    }

    [RelayCommand]
    private async Task MarkAsReadAsync(Guid id)
    {
        var item = Notifications.FirstOrDefault(x => x.Id == id);
        if (item is null || item.IsRead) return;

        // Optimistic update
        item.IsRead = true;

        var ok = await _gateway.MarkAsReadAsync(id);
        if (!ok)
            item.IsRead = false;
    }

    [RelayCommand]
    private async Task MarkAllReadAsync()
    {
        // Optimistic update
        var unread = Notifications.Where(x => !x.IsRead).ToList();
        foreach (var item in unread)
            item.IsRead = true;

        var ok = await _gateway.MarkAllAsReadAsync();
        if (!ok)
            foreach (var item in unread)
                item.IsRead = false;
    }
}
