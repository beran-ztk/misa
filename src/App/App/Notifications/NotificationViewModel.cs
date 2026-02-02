using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Notifications;

public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}

public sealed partial class NotificationItem : ObservableObject
{
    public NotificationItem(NotificationLevel level, string message)
    {
        Id = Guid.NewGuid();
        Level = level;
        Message = message;
        Timestamp = DateTimeOffset.Now;
    }

    public Guid Id { get; }
    public NotificationLevel Level { get; }
    public string Message { get; }
    public DateTimeOffset Timestamp { get; }

    [ObservableProperty] private bool _isRead;
    
    public void MarkRead() => IsRead = true;
}
public sealed partial class NotificationViewModel : ViewModelBase
{
    public ObservableCollection<NotificationItem> Notifications { get; } = [];
    public void Publish(NotificationLevel level, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, new NotificationItem(level, message));
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