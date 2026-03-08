using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IPanelHost
{
    Control? Panel { get; set; }
}

public partial class LayerProxy(
    IPanelHost panelHost,
    IServiceProvider sp) : ILayerCloser
{
    private TaskCompletionSource<object?>? _activePanelTcs;

    ICommand ILayerCloser.BackdropCloseCommand => BackdropCloseCommand;

    [RelayCommand]
    private void BackdropClose() => Close(null);

    [RelayCommand]
    public void Close(object? result = null)
    {
        panelHost.Panel = null;

        _activePanelTcs?.TrySetResult(result);
        _activePanelTcs = null;
    }

    public async Task<TResult?> OpenAsync<TForm, TResult>(TForm form)
        where TForm : ViewModelBase, IHostedForm<TResult>
    {
        var control = CreateHosted<TForm, TResult>(form, this);

        var bridgeTcs = new TaskCompletionSource<object?>();
        _activePanelTcs = bridgeTcs;

        panelHost.Panel = control;

        var completed = await bridgeTcs.Task;

        panelHost.Panel = null;
        _activePanelTcs = null;

        return completed is null
            ? default
            : (TResult)completed;
    }

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