using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class UtilityNavigationViewModel : ViewModelBase
{
    [RelayCommand]
    private void ToggleNotifications()
    {
        
    }
}