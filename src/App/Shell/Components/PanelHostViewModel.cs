using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class PanelHostViewModel<TResult>(
    Control contentView,
    object form,
    TaskCompletionSource<TResult?> tcs,
    IPanelCloser panelCloser)
    : ViewModelBase, IPanelHostViewModel
{
    public Control ContentView { get; } = contentView;
    private object Form { get; } = form;
    
    public string Title =>
        Form switch
        {
            IHostedForm<TResult> f => f.Title,
            _ => string.Empty
        };

    public string SubmitText => 
        Form switch
        {
            IHostedForm<TResult> f => f.SubmitText,
            _ => "Submit"
        };
    public string CancelText =>
        Form switch
        {
            IHostedForm<TResult> f => f.CancelText,
            _ => "Cancel"
        };

    public bool CanSubmit =>
        Form switch
        {
            IHostedForm<TResult> f => f.CanSubmit,
            _ => false
        };

    [RelayCommand]
    private void Close()
    {
        switch (Form)
        {
            case IHostedForm<TResult>:
            {
                tcs.TrySetResult(default);
                panelCloser.Close();
                return;
            }
        }
    }

    [RelayCommand]
    private void Cancel() => Close();

    [RelayCommand]
    private async Task Submit()
    {
        switch (Form)
        {
            case IHostedForm<TResult> form:
            {
                var result = await form.SubmitAsync();
                if (!result.IsSuccess)
                    return;

                tcs.TrySetResult(result.Value);
                panelCloser.Close();
                return;
            }
        }
    }
}