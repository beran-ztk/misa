using System;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.Composition;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.UI;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Shell.Base;

public partial class ShellWindowViewModel : ViewModelBase
{
    public ShellState ShellState { get; }
    private IServiceProvider ServiceProvider { get; }
    public required IPanelCloser PanelCloser { get; init; }
    public ShellWindowViewModel(ShellState shellState, IServiceProvider serviceProvider)
    {
        ShellState = shellState; 
        ServiceProvider = serviceProvider;
        
        PanelCloser = ServiceProvider.GetRequiredService<IPanelCloser>();
        
        ShellState.Header = ServiceProvider.GetRequiredService<HeaderViewModel>(); 
        ShellState.Footer = ServiceProvider.GetRequiredService<FooterViewModel>(); 
        ShellState.WorkspaceNavigation = ServiceProvider.GetRequiredService<WorkspaceNavigationViewModel>(); 
        ShellState.Inspector = ServiceProvider.GetRequiredService<InspectorFacadeViewModel>(); 
        ShellState.UtilityNavigation = ServiceProvider.GetRequiredService<UtilityNavigationViewModel>();
    }
}