using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Navigation;
using Misa.Ui.Avalonia.Infrastructure.States;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class UtilityNavigationViewModel(
    ShellState shellState,
    NotificationViewModel notificationVm
    ) : ViewModelBase
{
    [RelayCommand]
    private void ToggleNotifications()
    {
        if (shellState.Utility is NotificationViewModel)
        {
            shellState.Utility = null;
            return;
        }

        shellState.Utility = notificationVm;
    }
}