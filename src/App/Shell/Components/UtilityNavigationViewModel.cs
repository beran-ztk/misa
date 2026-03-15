using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class UtilityNavigationViewModel(ShellState shellState, NotificationViewModel notificationViewModel) : ViewModelBase
{
    public NotificationViewModel NotificationViewModel { get; } = notificationViewModel;
    [RelayCommand]
    private async Task ToggleNotifications()
    {
        if (shellState.Utility is NotificationViewModel)
        {
            shellState.Utility = null;
            return;
        }
        
        await notificationViewModel.InitializeAsync();
        shellState.Utility = notificationViewModel;
    }
}