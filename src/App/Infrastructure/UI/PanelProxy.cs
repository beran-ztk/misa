using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IPanelHost
{
    Control? Panel { get; set; }
}
public partial class PanelProxy(IPanelHost panelHost, IPanelFactory panelFactory) : IPanelCloser
{
    private TaskCompletionSource<object?>? _activePanelTcs;

    ICommand IPanelCloser.BackdropCloseCommand => BackdropCloseCommand;
    [RelayCommand] private void BackdropClose() => Close(null);
    
    [RelayCommand]
    public void Close(object? result = null)
    {
        panelHost.Panel = null;
        
        _activePanelTcs?.TrySetResult(result);
        _activePanelTcs = null;
    }

    public async Task<Result> OpenAsync(PanelKey key, object? context)
    {
        var (control, task) = panelFactory.CreateHosted(key, context);
        
        var bridgeTcs = new TaskCompletionSource<object?>();
        _activePanelTcs = bridgeTcs;
        
        panelHost.Panel = control;
        
        var completed = await Task.WhenAny(task, bridgeTcs.Task);
        
        panelHost.Panel = null;
        _activePanelTcs = null;
        
        if (completed == bridgeTcs.Task)
            return Result.Failure("Canceled.");

        return await task;
    }
    public async Task<TResult?> OpenAsync<TResult>(PanelKey<TResult> key, IHostedForm<TResult>? context)
    {
        var control = panelFactory.CreateHosted(key, context, this);
        
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
}