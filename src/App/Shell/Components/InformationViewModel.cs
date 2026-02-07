using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Navigation;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class InformationViewModel(INavigationService navigationService) : ViewModelBase
{
    [RelayCommand]
    private void ToggleNotifications()
    {
        if (navigationService.NavigationStore.CurrentOverlay is NotificationViewModel)
        {
            navigationService.NavigationStore.CurrentOverlay = null;
            return;
        }

        navigationService.NavigationStore.CurrentOverlay =
            navigationService.ServiceProvider.GetRequiredService<NotificationViewModel>();
    }
}