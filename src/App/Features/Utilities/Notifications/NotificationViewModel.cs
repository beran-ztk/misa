using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Messaging;
using Misa.Ui.Avalonia.Common.Mapping;

namespace Misa.Ui.Avalonia.Features.Utilities.Notifications;

public sealed partial class NotificationItem : ObservableObject
{
    public NotificationItem(NotificationDto notification)
    {
        Type = notification.NotificationType;
        Severity = notification.NotificationSeverity;
        Payload = notification.Payload;
        Timestamp = notification.Timestamp;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public NotificationTypeDto Type { get; private set; }
    public string TypeToString => Type.ToString();
    public NotificationSeverityDto Severity { get; private set; }
    public string SeverityToString => Severity.ToString();
    public string Payload { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    [ObservableProperty] private bool _isRead;
    
    public void MarkRead() => IsRead = true;
}
public sealed partial class NotificationViewModel : ViewModelBase
{
    public ObservableCollection<NotificationItem> Notifications { get; } = [];
    public void Publish(NotificationDto notification)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, new NotificationItem(notification));
        });
    }
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