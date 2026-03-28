using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector;
using Misa.Ui.Avalonia.Infrastructure;
using Misa.Ui.Avalonia.Shell.Components;
using ShellState = Misa.Ui.Avalonia.Infrastructure.ShellState;

namespace Misa.Ui.Avalonia.Shell;

public partial class ShellWindowViewModel : ViewModelBase
{
    public ShellState                 ShellState         { get; }
    public WorkspaceNavigationViewModel WorkspaceNavigation { get; }
    public UtilityNavigationViewModel  UtilityNavigation  { get; }
    private IServiceProvider ServiceProvider { get; }
    private readonly SelectedItemCoordinator _selectionContext;

    public ShellWindowViewModel(ShellState shellState, SelectedItemCoordinator selectionContext, IServiceProvider serviceProvider)
    {
        ShellState      = shellState;
        ServiceProvider = serviceProvider;
        _selectionContext  = selectionContext;

        ShellState.Header              = ServiceProvider.GetRequiredService<HeaderViewModel>();
        ShellState.Footer              = ServiceProvider.GetRequiredService<FooterViewModel>();
        ShellState.Inspector           = ServiceProvider.GetRequiredService<InspectorViewModel>();

        WorkspaceNavigation            = ServiceProvider.GetRequiredService<WorkspaceNavigationViewModel>();
        ShellState.WorkspaceNavigation = WorkspaceNavigation;

        UtilityNavigation             = ServiceProvider.GetRequiredService<UtilityNavigationViewModel>();
        ShellState.UtilityNavigation  = UtilityNavigation;
    }

    /// <summary>
    /// Escape handler: closes the topmost dismissible layer (modal → panel → utility panel → inspector).
    /// </summary>
    [RelayCommand]
    private void Dismiss()
    {
        if (ShellState.Utility is not null)
        {
            ShellState.Utility = null;
            return;
        }

        if (_selectionContext.ActiveEntityId is not null)
            _selectionContext.Clear();
    }
}