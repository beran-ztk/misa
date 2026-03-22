using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
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
    ObservableCollection<ToastViewModel> Toasts       { get; }
    ObservableCollection<ToastViewModel> ActionToasts { get; }
}

public partial class LayerProxy(
    ILayerHost       layerHost,
    IToastHost       toastHost,
    IServiceProvider sp) : ILayerCloser
{
    private TaskCompletionSource<object?>? _activePanelTcs;

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

    // ── Toast (side — persistent system notifications) ────────────────────

    public void ShowToast(string title, string? message = null, ToastType type = ToastType.Info, int durationMs = 4000)
    {
        var toasts = toastHost.Toasts;

        ToastViewModel? vm = null;
        vm = new ToastViewModel(title, message, () => toasts.Remove(vm!), type, durationMs);
        toasts.Add(vm);
    }

    // ── Action toast (top-center — transient user feedback) ───────────────
    // At most one item is shown at a time; a new call replaces the current one.

    public void ShowActionToast(string title, ToastType type = ToastType.Info, int durationMs = 3000, Action? undo = null)
    {
        var toasts = toastHost.ActionToasts;

        // Dismiss any currently visible toast immediately.
        foreach (var existing in toasts.ToList())
            existing.Dismiss();

        ToastViewModel? vm = null;
        vm = new ToastViewModel(title, null, () => toasts.Remove(vm!), type, durationMs, undo);
        toasts.Add(vm);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private LayerHostView CreateHosted<TForm, TResult>(
        TForm        form,
        ILayerCloser layerCloser)
        where TForm : ViewModelBase, IHostedForm<TResult>
    {
        var hostView = sp.GetRequiredService<LayerHostView>();
        hostView.DataContext = new LayerHostViewModel<TForm, TResult>(form, layerCloser);
        return hostView;
    }
}
