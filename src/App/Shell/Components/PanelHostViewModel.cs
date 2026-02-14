using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Shell.Components;

public partial class PanelHostViewModel<TResult>(
    IOverlayCloser closer,
    Control contentView,
    object form,
    TaskCompletionSource<TResult?> tcs)
    : ViewModelBase, IPanelHostViewModel
{
    public Control ContentView { get; } = contentView;
    private object Form { get; } = form;
    
    public string Title =>
        Form switch
        {
            IHostedForm<TResult> f => f.Title,
            IHostedCommitForm<TResult> f => f.Title,
            _ => string.Empty
        };

    public string SubmitText => 
        Form switch
        {
            IHostedForm<TResult> f => f.SubmitText,
            IHostedCommitForm<TResult> f => f.SubmitText,
            _ => "Submit"
        };
    public string CancelText =>
        Form switch
        {
            IHostedForm<TResult> f => f.CancelText,
            IHostedCommitForm<TResult> f => f.CancelText,
            _ => "Cancel"
        };

    public bool CanSubmit =>
        Form switch
        {
            IHostedForm<TResult> f => f.CanSubmit,
            IHostedCommitForm<TResult> f => f.CanSubmit,
            _ => false
        };

    [RelayCommand]
    private void Close()
    {
        tcs.TrySetResult(default);
        closer.ClosePanel();
    }

    [RelayCommand]
    private void Cancel() => Close();

    [RelayCommand]
    private async Task Submit()
    {
        switch (Form)
        {
            case IHostedForm<TResult> collectForm:
            {
                var value = collectForm.TrySubmit();
                if (value is null)
                    return;

                tcs.TrySetResult(value);
                closer.ClosePanel();
                break;
            }
            case IHostedCommitForm<TResult> commitForm:
            {
                var result = await commitForm.SubmitAsync();
                if (!result.IsSuccess)
                    return;

                tcs.TrySetResult(result.Value);
                closer.ClosePanel();
                return;
            }
        }
    }
}