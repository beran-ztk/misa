using System;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
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
    ScheduleFacadeViewModel schedule)
{
    public async Task Show(WorkspaceKind kind)
    {
        switch (kind)
        {
            case WorkspaceKind.Tasks:
                await task.InitializeWorkspaceAsync();
                shell.Workspace = task;
                break;
            case WorkspaceKind.Scheduler:
                await schedule.InitializeWorkspaceAsync();
                shell.Workspace = schedule;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}