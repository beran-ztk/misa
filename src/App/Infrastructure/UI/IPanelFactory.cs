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
        var hostView = sp.GetRequiredService<PanelHostView>();
        var closer = sp.GetRequiredService<IOverlayCloser>();

        switch (key)
        {
            case PanelKey.Task:
            {
                var body = sp.GetRequiredService<CreateTaskView>();

                var formVm = (context as IHostedForm<TResult>) 
                             ?? sp.GetRequiredService<CreateTaskViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;
                
                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);
                
                return (hostView, tcs.Task);
            }
            case PanelKey.Schedule:
            {
                var body = sp.GetRequiredService<CreateScheduleView>();

                var formVm = (context as IHostedForm<TResult>)
                             ?? sp.GetRequiredService<CreateScheduleViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }
            case PanelKey.StartSession:
            {
                var body = sp.GetRequiredService<StartSessionView>();

                var formVm = (context as IHostedForm<TResult>)
                             ?? sp.GetRequiredService<StartSessionViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }
            case PanelKey.PauseSession:
            {
                var body = sp.GetRequiredService<PauseSessionView>();

                var formVm = (context as IHostedForm<TResult>)
                             ?? sp.GetRequiredService<PauseSessionViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }
            case PanelKey.EndSession:
            {
                var body = sp.GetRequiredService<EndSessionView>();

                var formVm = (context as IHostedForm<TResult>)
                             ?? sp.GetRequiredService<EndSessionViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }
            case PanelKey.UpsertDeadline:
            {
                var body = sp.GetRequiredService<UpsertDeadlineView>();

                var formVm = (context as IHostedForm<TResult>)
                             ?? sp.GetRequiredService<UpsertDeadlineViewModel>() as IHostedForm<TResult>;
                if (formVm == null) throw new ArgumentNullException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new PanelHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}