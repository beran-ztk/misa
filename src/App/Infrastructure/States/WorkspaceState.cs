using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class WorkspaceState : ObservableObject
{
    [ObservableProperty] private Control _content;

    [ObservableProperty] private Control? _toolbar;
    [ObservableProperty] private ViewModelBase? _navigation;
    [ObservableProperty] private ViewModelBase? _contextPanel;
    [ObservableProperty] private ViewModelBase? _statusBar;
}
