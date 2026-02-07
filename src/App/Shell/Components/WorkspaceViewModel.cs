using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Shell.Components;

public class WorkspaceViewModel(WorkspaceState workspaceState) : ViewModelBase
{
    public WorkspaceState WorkspaceState => workspaceState;
}