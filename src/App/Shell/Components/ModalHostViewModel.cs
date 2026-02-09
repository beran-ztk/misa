using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class ModalHostViewModel<TResult> : ViewModelBase, IPanelHostViewModel
{
    private readonly IOverlayCloser _closer;
    private readonly TaskCompletionSource<TResult?> _tcs;

    public ModalHostViewModel(
        IOverlayCloser closer,
        Control contentView,
        IHostedForm<TResult> form,
        TaskCompletionSource<TResult?> tcs)
    {
        _closer = closer;
        _tcs = tcs;
        ContentView = contentView;
        Form = form;
    }

    public Control ContentView { get; }
    public IHostedForm<TResult> Form { get; }

    public string Title => Form.Title;
    public string SubmitText => Form.SubmitText;
    public string CancelText => Form.CancelText;
    public bool CanSubmit => Form.CanSubmit;

    [RelayCommand]
    private void Close()
    {
        _tcs.TrySetResult(default);
        _closer.CloseModal();
    }

    [RelayCommand] private void Cancel() => Close();

    [RelayCommand]
    private void Submit()
    {
        var result = Form.TrySubmit();
        if (result is null) return;

        _tcs.TrySetResult(result);
        _closer.CloseModal();
    }
}