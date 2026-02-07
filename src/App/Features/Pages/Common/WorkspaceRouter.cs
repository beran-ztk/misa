using System;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Header;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Main;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Header;
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
    
    TaskCoordinator taskVm)
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
                ws.Toolbar = null;
                ws.Navigation = null;
                ws.Content = sp.GetRequiredService<SchedulerMainWindowViewModel>();
                ws.ContextPanel = null;
                ws.StatusBar = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}