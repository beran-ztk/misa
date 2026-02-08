using System;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Common;

public enum WorkspaceKind
{
    Tasks,
    Scheduler
}

public sealed class WorkspaceRouter(
    ShellState shell, 
    IServiceProvider sp,
    
    TaskCoordinator taskVm,
    SchedulerCoordinator scheduleVm)
{
    public void Show(WorkspaceKind kind)
    {
        var ws = shell.WorkspaceState;

        switch (kind)
        {
            case WorkspaceKind.Tasks:
                taskVm.Attach(ws);
                break;
            case WorkspaceKind.Scheduler:
                scheduleVm.Attach(ws);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}