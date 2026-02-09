using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IOverlayCloser
{
    void ClosePanel();
    void CloseModal();
}
public sealed class OverlayCloser(ShellState shellState) : IOverlayCloser
{
    public void ClosePanel() => shellState.Panel = null;
    public void CloseModal() => shellState.Modal = null;
}