using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.App.Notifications;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

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