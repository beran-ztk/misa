using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Navigation;
using SchedulerMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Scheduling.Main.SchedulerMainWindowViewModel;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class WorkspaceNavigationViewModel(INavigationService navigationService) : ViewModelBase
{
    [RelayCommand]
    private void ShowTasks()
    {
        navigationService.NavigationStore.CurrentViewModel =
            navigationService.ServiceProvider.GetRequiredService<TaskMainWindowViewModel>();
    }

    [RelayCommand]
    private void OpenSchedulerPanel() =>
        navigationService.NavigationStore.CurrentViewModel =
            navigationService.ServiceProvider.GetRequiredService<SchedulerMainWindowViewModel>();
}