using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        Type      = notification.NotificationType;
        Severity  = notification.NotificationSeverity;
        Payload   = notification.Payload;
        Timestamp = notification.Timestamp;
    }

    public NotificationItem(NotificationEntryDto dto)
    {
        Id        = dto.Id;
        Type      = NotificationTypeDto.TaskCreated;
        Severity  = NotificationSeverityDto.Info;
        Payload   = string.IsNullOrWhiteSpace(dto.Message) ? dto.Title : $"{dto.Title}: {dto.Message}";
        Timestamp = dto.CreatedAtUtc;
    }

    public Guid                    Id              { get; }
    public NotificationTypeDto     Type            { get; private set; }
    public string                  TypeToString    => Type.ToString();
    public NotificationSeverityDto Severity        { get; private set; }
    public string                  SeverityToString => Severity.ToString();
    public string                  Payload         { get; private set; }
    public DateTimeOffset          Timestamp       { get; private set; }

    [ObservableProperty] private bool _isRead;

    public void MarkRead() => IsRead = true;
}

public sealed partial class NotificationViewModel : ViewModelBase
{
    private readonly NotificationGateway _gateway;

    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public NotificationViewModel(NotificationGateway gateway)
    {
        _gateway = gateway;
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
    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void Clear() => Notifications.Clear();

    [RelayCommand]
    private void MarkAllRead()
    {
        foreach (var n in Notifications)
            n.MarkRead();
    }

    [RelayCommand]
    private void Dismiss(Guid id)
    {
        var item = Notifications.FirstOrDefault(x => x.Id == id);
        if (item != null)
            Notifications.Remove(item);
    }
}
