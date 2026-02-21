using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Shell.Components;
public sealed partial class PanelHostViewModel(
    Control contentView,
    IHostedForm form,
    TaskCompletionSource<Result> tcs,
    IPanelCloser panelCloser)
    : ViewModelBase, IPanelHostViewModel
{
    public Control ContentView { get; } = contentView;
    private IHostedForm Form { get; } = form;

    public string Title => Form.Title;
    public string SubmitText => Form.SubmitText;
    public string CancelText => Form.CancelText;
    public bool CanSubmit => Form.CanSubmit;

    [RelayCommand]
    private void Close()
    {
        tcs.TrySetResult(Result.Failure("Closed."));
        panelCloser.Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

    [RelayCommand]
    private async Task Submit()
    {
        var result = await Form.SubmitAsync();
        if (!result.IsSuccess)
            return;

        tcs.TrySetResult(result);
        panelCloser.Close();
    }
}

public sealed partial class PanelHostViewModel<TResult>(
    Control contentView,
    IHostedForm<TResult> form,
    IPanelCloser panelCloser)
    : ViewModelBase, IPanelHostViewModel
{
    public Control ContentView { get; } = contentView;
    private IHostedForm<TResult> Form { get; } = form;

    public string Title => Form.Title;
    public string SubmitText => Form.SubmitText;
    public string CancelText => Form.CancelText;
    public bool CanSubmit => Form.CanSubmit;

    [RelayCommand]
    private void Close()
    {
        panelCloser.Close();
    }

    [RelayCommand]
    private void Cancel() => Close();

    [RelayCommand]
    private async Task Submit()
    {
        var result = await Form.SubmitAsync();
        if (!result.IsSuccess)
            return;

        panelCloser.Close(result.Value);
    }
}