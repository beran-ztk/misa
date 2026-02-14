using System.Threading.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public class PanelProxy(ShellState shellState, IPanelFactory panelFactory) : IPanelCloser
{
    private TaskCompletionSource<object?>? _activePanelTcs;
    
    public void Close()
    {
        shellState.Panel = null;
        _activePanelTcs?.TrySetResult(null);
        _activePanelTcs = null;
    }

    public async Task<Result> OpenAsync(PanelKey key, object? context)
    {
        var (control, task) = panelFactory.CreateHosted(key, context);
        
        var bridgeTcs = new TaskCompletionSource<object?>();
        _activePanelTcs = bridgeTcs;
        
        shellState.Panel = control;
        
        var completed = await Task.WhenAny(task, bridgeTcs.Task);
        
        shellState.Panel = null;
        _activePanelTcs = null;
        
        if (completed == bridgeTcs.Task)
            return Result.Failure("Canceled.");

        return await task;
    }
    public async Task<TResult?> OpenAsync<TResult>(PanelKey<TResult> key, IHostedForm<TResult>? context)
    {
        var (control, task) = panelFactory.CreateHosted(key, context);
        
        var bridgeTcs = new TaskCompletionSource<object?>();
        _activePanelTcs = bridgeTcs;
        
        shellState.Panel = control;
        
        var completed = await Task.WhenAny(task, bridgeTcs.Task);
        
        shellState.Panel = null;
        _activePanelTcs = null;
        
        if (completed == bridgeTcs.Task)
            return default;
        
        return await task;
    }
}