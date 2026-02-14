using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Scheduling.Forms;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum PanelKey
{
    Task,
    Schedule,
    StartSession,
    PauseSession,
    EndSession,
    UpsertDeadline
}

public interface IPanelFactory
{
    (Control Control, Task ResultTask) CreateHosted(PanelKey key, object? context);
    (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(PanelKey key, object? context);
}

public sealed class PanelFactory(IServiceProvider sp) : IPanelFactory
{
    public (Control Control, Task ResultTask) CreateHosted(PanelKey key, object? context)
    {
        return key switch
        {
            PanelKey.EndSession => CreateHosted<EndSessionView, EndSessionViewModel>(context),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
    public (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(PanelKey key, object? context)
    {
        return key switch
        {
            PanelKey.Task           => CreateHosted<TResult, CreateTaskView,          CreateTaskViewModel>(context),
            PanelKey.Schedule       => CreateHosted<TResult, CreateScheduleView,      CreateScheduleViewModel>(context),
            PanelKey.StartSession   => CreateHosted<TResult, StartSessionView,        StartSessionViewModel>(context),
            PanelKey.PauseSession   => CreateHosted<TResult, PauseSessionView,        PauseSessionViewModel>(context),
            PanelKey.UpsertDeadline => CreateHosted<TResult, UpsertDeadlineView,      UpsertDeadlineViewModel>(context),
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
    private (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult, TView, TViewModel>(object? context)
        where TView : Control
        where TViewModel : class, IHostedForm<TResult>
    {
        var hostView = sp.GetRequiredService<PanelHostView>();
        var panelCloser = sp.GetRequiredService<IPanelCloser>();

        var body = sp.GetRequiredService<TView>();
        var formVm = context as IHostedForm<TResult> ?? sp.GetRequiredService<TViewModel>();
        
        body.DataContext = formVm;
        
        var tcs = new TaskCompletionSource<TResult?>();
        hostView.DataContext = new PanelHostViewModel<TResult>(body, formVm, tcs, panelCloser);
        
        return (hostView, tcs.Task);
    }
}