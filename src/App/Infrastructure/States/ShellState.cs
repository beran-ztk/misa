using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Main;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class ShellState(IServiceProvider  serviceProvider) : ObservableObject
{
    [ObservableProperty] private ViewModelBase _header = serviceProvider.GetRequiredService<HeaderViewModel>();
    [ObservableProperty] private ViewModelBase _footer = serviceProvider.GetRequiredService<FooterViewModel>();
    
    [ObservableProperty] private ViewModelBase _workspaceNavigation = serviceProvider.GetRequiredService<WorkspaceNavigationViewModel>();
    [ObservableProperty] private ViewModelBase? _workspace;
    
    [ObservableProperty] private ViewModelBase _utilityNavigation = serviceProvider.GetRequiredService<UtilityNavigationViewModel>();
    [ObservableProperty] private ViewModelBase? _utility;
    
    
    [ObservableProperty] private ViewModelBase? _panel;
    [ObservableProperty] private ViewModelBase? _modal;
    [ObservableProperty] private ViewModelBase? _toast;
    [ObservableProperty] private ViewModelBase? _busy;
}