using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Utilities.Dev;
using ShellState = Misa.Ui.Avalonia.Infrastructure.ShellState;

namespace Misa.Ui.Avalonia.Shell.Components;

public sealed partial class UtilityNavigationViewModel(ShellState shellState, DevToolsViewModel devToolsViewModel)
    : ViewModelBase
{
    [RelayCommand]
    private void ToggleDevTools()
    {
        if (shellState.Utility is DevToolsViewModel)
        {
            shellState.Utility = null;
            return;
        }

        shellState.Utility = devToolsViewModel;
    }
}
