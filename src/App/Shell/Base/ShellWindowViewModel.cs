using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Composition;

namespace Misa.Ui.Avalonia.Shell.Base;

public partial class ShellWindowViewModel : ViewModelBase
{
    public AppState AppState { get; init; }
    public ShellWindowViewModel(AppState appState)
    {
       AppState = appState;
    }
}