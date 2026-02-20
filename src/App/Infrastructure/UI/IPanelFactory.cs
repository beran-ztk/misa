using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Features.Chronicle;
using Misa.Contract.Features.Common.Deadlines;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Features.Pages.Chronicle.Create;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum PanelKey
{
    EndSession,
}

public readonly record struct PanelKey<TResult>(
    string Id,
    Func<IServiceProvider, Control> ViewFactory,
    Func<IServiceProvider, IHostedForm<TResult>> FormFactory
);
public static class Panels
{
    public static readonly PanelKey<TaskDto> Task =
        new("Task",
            sp => sp.GetRequiredService<CreateTaskView>(),
            sp => sp.GetRequiredService<CreateTaskViewModel>());

    public static readonly PanelKey<ScheduleDto> Schedule =
        new("Schedule",
            sp => sp.GetRequiredService<CreateScheduleView>(),
            sp => sp.GetRequiredService<CreateScheduleViewModel>());
    
    public static readonly PanelKey<JournalEntryDto> Journal =
        new("Journal",
            sp => sp.GetRequiredService<CreateJournalView>(),
            sp => sp.GetRequiredService<CreateJournalViewModel>());

    public static readonly PanelKey<SessionResolvedDto> StartSession =
        new("StartSession",
            sp => sp.GetRequiredService<StartSessionView>(),
            sp => sp.GetRequiredService<StartSessionViewModel>());

    public static readonly PanelKey<SessionResolvedDto> PauseSession =
        new("PauseSession",
            sp => sp.GetRequiredService<PauseSessionView>(),
            sp => sp.GetRequiredService<PauseSessionViewModel>());

    public static readonly PanelKey<DeadlineDto> UpsertDeadline =
        new("UpsertDeadline",
            sp => sp.GetRequiredService<UpsertDeadlineView>(),
            sp => sp.GetRequiredService<UpsertDeadlineViewModel>());
}

public interface IPanelFactory
{
    (Control Control, Task<Result> ResultTask) CreateHosted(PanelKey key, object? context);
    Control CreateHosted<TResult>(PanelKey<TResult> key, IHostedForm<TResult>? context, IPanelCloser panelCloser);
}

public sealed class PanelFactory(IServiceProvider sp) : IPanelFactory
{
    public (Control Control, Task<Result> ResultTask) CreateHosted(PanelKey key, object? context)
    {
        return key switch
        {
            PanelKey.EndSession => CreateHosted<EndSessionView, EndSessionViewModel>(context),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
    private (Control Control, Task<Result> ResultTask) CreateHosted<TView, TViewModel>(object? context)
        where TView : Control
        where TViewModel : class, IHostedForm
    {
        var hostView = sp.GetRequiredService<PanelHostView>();
        var panelCloser = sp.GetRequiredService<IPanelCloser>();

        var body = sp.GetRequiredService<TView>();
        var formVm = context as IHostedForm ?? sp.GetRequiredService<TViewModel>();
        
        body.DataContext = formVm;
        
        var tcs = new TaskCompletionSource<Result>();
        hostView.DataContext = new PanelHostViewModel(body, formVm, tcs, panelCloser);
        
        return (hostView, tcs.Task);
    }
    
    // Generic
    public Control CreateHosted<TResult>(
        PanelKey<TResult> key,
        IHostedForm<TResult>? context,
        IPanelCloser panelCloser)
    {
        var hostView = sp.GetRequiredService<PanelHostView>();

        var body = key.ViewFactory(sp);
        var formVm = context ?? key.FormFactory(sp);
        
        body.DataContext = formVm;
        
        hostView.DataContext = new PanelHostViewModel<TResult>(body, formVm, panelCloser);
        
        return hostView;
    }
}