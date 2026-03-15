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
    public ShellState                ShellState        { get; }
    public UtilityNavigationViewModel UtilityNavigation { get; }
    private IServiceProvider ServiceProvider { get; }
    public required ILayerCloser LayerCloser { get; init; }

    public ShellWindowViewModel(ShellState shellState, IServiceProvider serviceProvider)
    {
        ShellState      = shellState;
        ServiceProvider = serviceProvider;

        LayerCloser = ServiceProvider.GetRequiredService<ILayerCloser>();

        ShellState.Header             = ServiceProvider.GetRequiredService<HeaderViewModel>();
        ShellState.Footer             = ServiceProvider.GetRequiredService<FooterViewModel>();
        ShellState.WorkspaceNavigation = ServiceProvider.GetRequiredService<WorkspaceNavigationViewModel>();
        ShellState.Inspector          = ServiceProvider.GetRequiredService<InspectorFacadeViewModel>();

        UtilityNavigation             = ServiceProvider.GetRequiredService<UtilityNavigationViewModel>();
        ShellState.UtilityNavigation  = UtilityNavigation;
    }
}