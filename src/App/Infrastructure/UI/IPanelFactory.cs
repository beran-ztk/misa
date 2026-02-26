using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum PanelKey
{
    StartSession,
    PauseSession,
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
            PanelKey.StartSession => CreateHosted<StartSessionView, StartSessionViewModel>(context),
            PanelKey.PauseSession => CreateHosted<PauseSessionView, PauseSessionViewModel>(context),
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