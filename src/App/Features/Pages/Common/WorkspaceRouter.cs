using System;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;
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
    
    TaskFacadeViewModel task,
    SchedulerFacadeViewModel schedule)
{
    public async Task Show(WorkspaceKind kind)
    {
        switch (kind)
        {
            case WorkspaceKind.Tasks:
                await task.InitializeWorkspace();
                shell.Workspace = task;
                break;
            case WorkspaceKind.Scheduler:
                await schedule.InitializeWorkspace();
                shell.Workspace = schedule;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}