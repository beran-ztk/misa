using Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Toolbar;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public class SchedulerCoordinator(SchedulerToolbarViewModel toolbarVm, SchedulerContentViewModel contentVm)
{
    private SchedulerToolbarViewModel Toolbar { get; } = toolbarVm;
    private SchedulerContentViewModel Content { get; } = contentVm;

    public void Attach(WorkspaceState ws)
    {
        ws.Toolbar = Toolbar;
        ws.Navigation = null;
        ws.Content = Content;
        ws.ContextPanel = null;
        ws.StatusBar = null;
    }
}