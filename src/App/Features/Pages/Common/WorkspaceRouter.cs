using System;
using System.Threading.Tasks;
using Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Common;

public enum WorkspaceKind
{
    Tasks,
    Scheduler,
    Chronicle
}

public sealed class WorkspaceRouter(
    ShellState shell, 
    
    TaskFacadeViewModel task,
    SchedulerFacadeViewModel schedule,
    ChronicleViewModel chronicle)
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
            case WorkspaceKind.Chronicle:
                await chronicle.InitializeWorkspaceAsync();
                shell.Workspace = chronicle;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}