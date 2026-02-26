using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

namespace Misa.Ui.Avalonia.Shell.Components;

public interface IWorkspaceHost
{
    ViewModelBase? Workspace { get; set; }
}

public partial class WorkspaceNavigationViewModel : ViewModelBase
{
    private readonly IWorkspaceHost _host;
    private readonly TaskFacadeViewModel _task;
    private readonly ScheduleFacadeViewModel _schedule;

    public WorkspaceNavigationViewModel(
        IWorkspaceHost host,
        TaskFacadeViewModel task,
        ScheduleFacadeViewModel schedule)
    {
        _host = host;
        _task = task;
        _schedule = schedule;
    }

    [RelayCommand]
    private async Task ShowTasks()
    {
        await _task.InitializeWorkspaceAsync();
        _host.Workspace = _task;
    }

    [RelayCommand]
    private async Task ShowScheduler()
    {
        await _schedule.InitializeWorkspaceAsync();
        _host.Workspace = _schedule;
    }
}