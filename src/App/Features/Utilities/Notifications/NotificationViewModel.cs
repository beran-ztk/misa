using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    }

    public NotificationItem(NotificationEntryDto dto)
    {
        Id        = dto.Id;
        Title     = dto.Title;
        Message   = dto.Message;
        Timestamp = dto.CreatedAtUtc;
    }

    public Guid           Id                 { get; }
    public string         Title              { get; }
    public string         Message            { get; }
    public bool           HasMessage         => !string.IsNullOrWhiteSpace(Message);
    public DateTimeOffset Timestamp          { get; }
    public string         TimestampFormatted => Timestamp.ToLocalTime().ToString("dd MMM · HH:mm", CultureInfo.InvariantCulture);
}

public sealed partial class NotificationViewModel : ViewModelBase
{
    private readonly NotificationGateway _gateway;

    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public bool IsEmpty => Notifications.Count == 0;

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

    public async Task LoadAsync(CancellationToken ct = default)
    {
        var dtos = await _gateway.GetAllAsync(ct) ?? [];

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var existingIds = Notifications.Select(x => x.Id).ToHashSet();
            foreach (var dto in dtos)
            {
                if (existingIds.Add(dto.Id))
                    Notifications.Add(new NotificationItem(dto));
            }
        });
    }

    public async Task InitializeAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

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
}
