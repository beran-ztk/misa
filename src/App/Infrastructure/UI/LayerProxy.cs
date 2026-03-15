using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Utilities.Toast;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public enum LayerPresentation
{
    Panel,
    Modal
}

public interface ILayerHost
{
    Control? Panel { get; set; }
    Control? Modal { get; set; }
}

public interface IToastHost
{
    ViewModelBase? Toast { get; set; }
}

public partial class LayerProxy(
    ILayerHost     layerHost,
    IToastHost     toastHost,
    IServiceProvider sp) : ILayerCloser
{
    private TaskCompletionSource<object?>? _activePanelTcs;
    private CancellationTokenSource?      _toastCts;

    ICommand ILayerCloser.BackdropCloseCommand => BackdropCloseCommand;

    [RelayCommand]
    private void BackdropClose() => Close(null);

    [RelayCommand]
    public void Close(object? result = null)
    {
        if (layerHost.Modal is not null)
            layerHost.Modal = null;
        else if (layerHost.Panel is not null)
            layerHost.Panel = null;

        _activePanelTcs?.TrySetResult(result);
        _activePanelTcs = null;
    }

    public async Task<TResult?> OpenAsync<TForm, TResult>(TForm form, LayerPresentation mode = LayerPresentation.Panel)
        where TForm : ViewModelBase, IHostedForm<TResult>
    {
        var control = CreateHosted<TForm, TResult>(form, this);

        var bridgeTcs = new TaskCompletionSource<object?>();
        _activePanelTcs = bridgeTcs;

        switch (mode)
        {
            case LayerPresentation.Panel:
                layerHost.Panel = control;
                break;
            case LayerPresentation.Modal:
                layerHost.Modal = control;
                break;
        }

        var completed = await bridgeTcs.Task;

        _activePanelTcs = null;
        switch (mode)
        {
            case LayerPresentation.Panel:
                layerHost.Panel = null;
                break;
            case LayerPresentation.Modal:
                layerHost.Modal = null;
                break;
        }

        return completed is null
            ? default
            : (TResult)completed;
    }

    // ── Toast ─────────────────────────────────────────────────────────────

    public void ShowToast(string title, string? message = null, int durationMs = 4000)
    {
        // Cancel any existing auto-dismiss timer and replace the toast.
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var cts = _toastCts;

        void DismissAction()
        {
            cts.Cancel();
            toastHost.Toast = null;
        }

        toastHost.Toast = new ToastViewModel(title, message, DismissAction);
        _ = ScheduleAutoDismissAsync(cts.Token, durationMs);
    }

    private async Task ScheduleAutoDismissAsync(CancellationToken ct, int durationMs)
    {
        try
        {
            await Task.Delay(durationMs, ct);
            await Dispatcher.UIThread.InvokeAsync(() => toastHost.Toast = null);
        }
        catch (OperationCanceledException)
        {
            // Manually dismissed or replaced by a new toast — nothing to do.
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private LayerHostView CreateHosted<TForm, TResult>(
        TForm form,
        ILayerCloser layerCloser)
        where TForm : ViewModelBase, IHostedForm<TResult>
    {
        var hostView = sp.GetRequiredService<LayerHostView>();
        hostView.DataContext = new LayerHostViewModel<TForm, TResult>(form, layerCloser);
        return hostView;
    }
}
