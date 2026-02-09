using System.Threading.Tasks;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public class PanelProxy(ShellState shellState, IPanelFactory panelFactory) : IOverlayCloser
{
    public void ClosePanel() => shellState.Panel = null;
    public void CloseModal() => shellState.Modal = null;

    public async Task<TResult?> OpenAsync<TResult>(PanelKey key, object? context)
    {
        var (control, task) = panelFactory.CreateHosted<TResult>(key, context);
        shellState.Panel = control;
        return await task;
    }
}