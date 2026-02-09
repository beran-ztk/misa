using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Toolbar;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed class TaskCoordinator(TaskToolbarView toolbarView, TaskContentView contentView)
{
    public void Attach(WorkspaceState ws)
    {
        ws.Toolbar = toolbarView;
        ws.Navigation = null;
        ws.Content = contentView;
        ws.ContextPanel = null;
        ws.StatusBar = null;
    }
}