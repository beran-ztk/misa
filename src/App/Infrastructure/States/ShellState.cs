using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class ShellState : ObservableObject
{
    
    [ObservableProperty] private ViewModelBase _header;
    [ObservableProperty] private ViewModelBase _footer;
    
    [ObservableProperty] private ViewModelBase _workspaceNavigation;
    [ObservableProperty] private ViewModelBase? _workspace;

    [ObservableProperty] private ViewModelBase _utilityNavigation;
    [ObservableProperty] private ViewModelBase? _utility;
    
    
    [ObservableProperty] private ViewModelBase? _panel;
    [ObservableProperty] private ViewModelBase? _modal;
    [ObservableProperty] private ViewModelBase? _toast;
    [ObservableProperty] private ViewModelBase? _busy;
}