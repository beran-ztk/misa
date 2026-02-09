using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public sealed class ModalProxy(ShellState shellState, IModalFactory modalFactory)
{
    public void Close() => shellState.Modal = null;
    public void Open(ModalKey key, object? context) => shellState.Modal = modalFactory.Create(key, context);
}