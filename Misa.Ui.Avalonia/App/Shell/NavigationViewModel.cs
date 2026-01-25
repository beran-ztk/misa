using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Features.Scheduler.Main;
using Misa.Ui.Avalonia.Features.Tasks.Page;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

public partial class NavigationViewModel(INavigationService navigationService) : ViewModelBase
{
    [RelayCommand]
    private void ShowTasks()
    {
        navigationService.NavigationStore.CurrentViewModel = new PageViewModel(navigationService);
    }

    [RelayCommand]
    private void OpenSchedulerPanel() =>
        navigationService.NavigationStore.CurrentViewModel = new SchedulerMainWindowViewModel(navigationService);
}