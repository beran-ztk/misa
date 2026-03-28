using System.Windows.Input;

namespace Misa.Ui.Avalonia.Infrastructure;

public interface ILayerCloser
{
    void Close(object? result = null);
    ICommand BackdropCloseCommand { get; }
}