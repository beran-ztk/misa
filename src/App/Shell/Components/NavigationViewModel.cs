using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mapping;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using SchedulerMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Scheduling.Main.SchedulerMainWindowViewModel;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class NavigationViewModel(INavigationService navigationService) : ViewModelBase
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