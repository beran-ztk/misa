using System;
using System.Data;
using System.Threading.Tasks;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum ModalKey
{
    Task
}
public interface IModalFactory
{
    (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(ModalKey key, object? context);
}
public sealed class ModalFactory(IServiceProvider sp) : IModalFactory
{

    public (Control Control, Task<TResult?> ResultTask) CreateHosted<TResult>(ModalKey key, object? context)
    {
        var hostView = sp.GetRequiredService<ModalHostView>();
        var closer = sp.GetRequiredService<IOverlayCloser>();

        switch (key)
        {
            case ModalKey.Task:
            {
                var body = sp.GetRequiredService<CreateTaskView>();

                var formVm =
                    (context as IHostedForm<TResult>)
                    ?? sp.GetRequiredService<CreateTaskReturnViewModel>() as IHostedForm<TResult>;
                if (formVm is null) throw new NoNullAllowedException();
                body.DataContext = formVm;

                var tcs = new TaskCompletionSource<TResult?>();
                hostView.DataContext = new ModalHostViewModel<TResult>(closer, body, formVm, tcs);

                return (hostView, tcs.Task);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}