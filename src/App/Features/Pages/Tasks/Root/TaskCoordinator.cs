using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Infrastructure.States;
using TaskToolbarViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Toolbar.TaskToolbarViewModel;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed  class TaskCoordinator(TaskToolbarViewModel toolbarVm, TaskContentViewModel contentVm)
{
    private TaskToolbarViewModel Toolbar { get; } = toolbarVm;
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