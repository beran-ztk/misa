using System.Threading.Tasks;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public sealed class ModalProxy(ShellState shellState, IModalFactory modalFactory)
{
    public void CloseModal() => shellState.Modal = null;

    public async Task<TResult?> OpenAsync<TResult>(ModalKey key, object? context)
    {
        var (control, task) = modalFactory.CreateHosted<TResult>(key, context);
        shellState.Modal = control;
        return await task;
    }
}