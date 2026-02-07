using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Common;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class WorkspaceNavigationViewModel(WorkspaceRouter router) : ViewModelBase
{
    [RelayCommand]
    private void ShowTasks() => router.Show(WorkspaceKind.Tasks);
    [RelayCommand]
    private void ShowScheduler() => router.Show(WorkspaceKind.Scheduler);
}