using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using SchedulerMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Scheduling.Main.SchedulerMainWindowViewModel;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class WorkspaceNavigationViewModel(
    ShellState shellState,
    TaskMainWindowViewModel taskVm,
    SchedulerMainWindowViewModel schedulerVm
    ) : ViewModelBase
{
    [RelayCommand]
    private void ShowTasks()
        => shellState.Workspace = taskVm;

    [RelayCommand]
    private void ShowScheduler()
        => shellState.Workspace = schedulerVm;
}