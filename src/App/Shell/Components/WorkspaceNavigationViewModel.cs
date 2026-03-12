using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

namespace Misa.Ui.Avalonia.Shell.Components;

public interface IWorkspaceHost
{
    ViewModelBase? Workspace { get; set; }
}

public partial class WorkspaceNavigationViewModel(
    IWorkspaceHost host,
    TaskFacadeViewModel task,
    ScheduleFacadeViewModel schedule,
    ChronicleViewModel chronicle,
    ZettelkastenViewModel zettel)
    : ViewModelBase
{
    [RelayCommand]
    private async Task ShowTasks()
    {
        await task.InitializeWorkspaceAsync();
        host.Workspace = task;
    }

    [RelayCommand]
    private async Task ShowScheduler()
    {
        await schedule.InitializeWorkspaceAsync();
        host.Workspace = schedule;
    }
    [RelayCommand]
    private async Task ShowChronicle()
    {
        await chronicle.InitializeWorkspaceAsync();
        host.Workspace = chronicle;
    }
    [RelayCommand]
    private async Task ShowZettelkasten()
    {
        await zettel.InitializeWorkspaceAsync();
        host.Workspace = zettel;
    }
}