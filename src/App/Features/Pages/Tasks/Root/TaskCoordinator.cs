using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Header;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed  class TaskCoordinator(TaskHeaderViewModel headerVm, TaskContentViewModel contentVm)
{
    private TaskHeaderViewModel Toolbar { get; } = headerVm;
    private TaskContentViewModel Content { get; } = contentVm;

    public void Attach(WorkspaceState ws)
    {
        ws.Toolbar = Toolbar;
        ws.Navigation = null;
        ws.Content = Content;
        ws.ContextPanel = null;
        ws.StatusBar = null;
    }
}