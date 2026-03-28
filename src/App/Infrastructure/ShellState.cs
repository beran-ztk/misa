using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure;

public partial class ShellState : ObservableObject, IWorkspaceHost
{
    private readonly ISelectionContextState _selectionContext;
    
    public ShellState(ISelectionContextState selectionContextState)
    {
        _selectionContext = selectionContextState;
       
        _selectionContext.PropertyChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(SelectionContextHasId));
        };
    }
    
    [ObservableProperty] private ViewModelBase _header;
    [ObservableProperty] private ViewModelBase _footer;
    
    [ObservableProperty] private ViewModelBase _workspaceNavigation;
    [ObservableProperty] private ViewModelBase? _workspace;
    
    [ObservableProperty] private ViewModelBase _inspector;
    public bool SelectionContextHasId => _selectionContext.ActiveEntityId is not null;
    
    [ObservableProperty] private ViewModelBase _utilityNavigation;
    [ObservableProperty] private ViewModelBase? _utility;
}