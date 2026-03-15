using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class UtilityNavigationViewModel : ViewModelBase
{
    private readonly ShellState            _shellState;
    private readonly NotificationViewModel _notificationViewModel;

    public NotificationViewModel NotificationViewModel => _notificationViewModel;

    public bool   HasUnread => _notificationViewModel.UnreadCount > 0;
    public string BadgeText => _notificationViewModel.UnreadCount > 9
        ? "9+"
        : _notificationViewModel.UnreadCount.ToString();

    public UtilityNavigationViewModel(ShellState shellState, NotificationViewModel notificationViewModel)
    {
        _shellState            = shellState;
        _notificationViewModel = notificationViewModel;

        _notificationViewModel.PropertyChanged += OnNotificationPropertyChanged;
    }

    private void OnNotificationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NotificationViewModel.UnreadCount)) return;
        OnPropertyChanged(nameof(HasUnread));
        OnPropertyChanged(nameof(BadgeText));
    }

    [RelayCommand]
    private async Task ToggleNotifications()
    {
        if (_shellState.Utility is NotificationViewModel)
        {
            _shellState.Utility = null;
            return;
        }

        await _notificationViewModel.InitializeAsync();
        _shellState.Utility = _notificationViewModel;
    }
}
