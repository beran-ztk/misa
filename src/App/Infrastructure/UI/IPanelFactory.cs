using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
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
    (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(PanelKey key, object? context);
}

public sealed class PanelFactory(IServiceProvider sp) : IPanelFactory
{
    public (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(PanelKey key, object? context)
    {
        return key switch
        {
            PanelKey.Task           => CreateHosted<TResult, CreateTaskView,          CreateTaskViewModel>(context),
            PanelKey.Schedule       => CreateHosted<TResult, CreateScheduleView,      CreateScheduleViewModel>(context),
            PanelKey.StartSession   => CreateHosted<TResult, StartSessionView,        StartSessionViewModel>(context),
            PanelKey.PauseSession   => CreateHosted<TResult, PauseSessionView,        PauseSessionViewModel>(context),
            PanelKey.EndSession     => CreateHosted<TResult, EndSessionView,          EndSessionViewModel>(context),
            PanelKey.UpsertDeadline => CreateHosted<TResult, UpsertDeadlineView,      UpsertDeadlineViewModel>(context),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
    private (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult, TView, TViewModel>(object? context)
        where TView : Control
        where TViewModel : class
    {
        var hostView = sp.GetRequiredService<PanelHostView>();
        var panelCloser = sp.GetRequiredService<IPanelCloser>();

        var body = sp.GetRequiredService<TView>();
        var formVm = context as TViewModel ?? sp.GetRequiredService<TViewModel>();
        
        body.DataContext = formVm;
        
        var tcs = new TaskCompletionSource<TResult?>();
        hostView.DataContext = new PanelHostViewModel<TResult>(body, formVm, tcs, panelCloser);
        
        return (hostView, tcs.Task);
    }
}