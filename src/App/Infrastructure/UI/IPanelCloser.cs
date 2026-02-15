using System.Windows.Input;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IPanelCloser
{
    void Close(object? result = null);
    ICommand BackdropCloseCommand { get; }
}