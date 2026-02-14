using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace Misa.Ui.Avalonia.Infrastructure.UI;

public interface IPanelHostViewModel
{
    Control ContentView { get; }

    string Title { get; }
    string SubmitText { get; }
    string CancelText { get; }
    bool CanSubmit { get; }

    IRelayCommand CloseCommand { get; }
    IRelayCommand CancelCommand { get; }
    IAsyncRelayCommand SubmitCommand { get; }
}