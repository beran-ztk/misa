using System;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Base;
using Misa.Ui.Avalonia.Infrastructure.Composition;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Shell.Base;

public partial class ShellWindowViewModel : ViewModelBase
{
    public AppState AppState { get; }
    private IServiceProvider ServiceProvider { get; }
    public ShellWindowViewModel(AppState appState, IServiceProvider serviceProvider)
    {
       AppState = appState;
       ServiceProvider = serviceProvider;
       
       AppState.ShellState.Header = ServiceProvider.GetRequiredService<HeaderViewModel>();
       AppState.ShellState.Footer = ServiceProvider.GetRequiredService<FooterViewModel>();
       AppState.ShellState.WorkspaceNavigation = ServiceProvider.GetRequiredService<WorkspaceNavigationViewModel>();
       AppState.ShellState.Inspector = ServiceProvider.GetRequiredService<InspectorViewModel>();
       AppState.ShellState.UtilityNavigation = ServiceProvider.GetRequiredService<UtilityNavigationViewModel>();
    }
}