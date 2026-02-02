using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.App.Notifications;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

public sealed partial class InformationViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }

    public InformationViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
    }

    [RelayCommand]
    private void ToggleNotifications()
    {
        var store = NavigationService.NavigationStore;

        if (store.CurrentOverlay is NotificationViewModel)
        {
            store.CurrentOverlay = null;
            return;
        }

        store.CurrentOverlay =
            NavigationService.ServiceProvider.GetRequiredService<NotificationViewModel>();
    }
}