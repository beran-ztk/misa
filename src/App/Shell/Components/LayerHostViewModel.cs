using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Shell.Components;

public interface IPanelHostViewModel
{
    string FormTitle { get; }
    string? FormDescription { get; }
    ViewModelBase Form { get; }
    IRelayCommand CloseCommand { get; }
    IAsyncRelayCommand SubmitCommand { get; }
}

public sealed partial class LayerHostViewModel<TForm, TResult>(
    TForm form,
    ILayerCloser layerCloser)
    : ViewModelBase, IPanelHostViewModel
    where TForm : ViewModelBase, IHostedForm<TResult>
{
    public ViewModelBase Form { get; } = form;
    public string FormTitle { get; } = form.FormTitle;
    public string? FormDescription { get; } = form.FormDescription;

    [RelayCommand]
    private async Task Submit()
    {
        var result = await form.SubmitAsync();

        if (!result.IsSuccess)
        {
            return;
        }

        layerCloser.Close(result.Value);
    }

    [RelayCommand]
    private void Close() => layerCloser.Close();
}