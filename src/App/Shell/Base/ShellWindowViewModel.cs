using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.UI;
using Misa.Ui.Avalonia.Shell.Components;
using InspectorFacadeViewModel = Misa.Ui.Avalonia.Features.Inspector.InspectorFacadeViewModel;
using ScheduleFacadeViewModel = Misa.Ui.Avalonia.Features.Pages.Schedules.ScheduleFacadeViewModel;
using TaskFacadeViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.TaskFacadeViewModel;

namespace Misa.Ui.Avalonia.Shell.Base;

public partial class ShellWindowViewModel : ViewModelBase
{
    public ShellState                 ShellState         { get; }
    public WorkspaceNavigationViewModel WorkspaceNavigation { get; }
    public UtilityNavigationViewModel  UtilityNavigation  { get; }
    private IServiceProvider ServiceProvider { get; }
    public required ILayerCloser LayerCloser { get; init; }
    private readonly ISelectionContextState _selectionContext;

    public ShellWindowViewModel(ShellState shellState, IServiceProvider serviceProvider)
    {
        ShellState      = shellState;
        ServiceProvider = serviceProvider;

        LayerCloser        = ServiceProvider.GetRequiredService<ILayerCloser>();
        _selectionContext  = ServiceProvider.GetRequiredService<ISelectionContextState>();

        ShellState.Header              = ServiceProvider.GetRequiredService<HeaderViewModel>();
        ShellState.Footer              = ServiceProvider.GetRequiredService<FooterViewModel>();
        ShellState.Inspector           = ServiceProvider.GetRequiredService<InspectorFacadeViewModel>();

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
        if (ShellState.IsModalOpen || ShellState.IsPanelOpen)
        {
            LayerCloser.Close();
            return;
        }

        if (ShellState.Utility is not null)
        {
            ShellState.Utility = null;
            return;
        }

        if (_selectionContext.ActiveEntityId is not null)
            _selectionContext.Clear();
    }

    /// <summary>
    /// Ctrl+Space: triggers the active workspace's primary Add action.
    /// </summary>
    [RelayCommand]
    private void WorkspaceAdd()
    {
        switch (ShellState.Workspace)
        {
            case TaskFacadeViewModel vm:     vm.ShowAddPanelCommand.Execute(null); break;
            case ScheduleFacadeViewModel vm: vm.ShowAddPanelCommand.Execute(null); break;
            case ChronicleViewModel vm:      vm.ShowAddPanelCommand.Execute(null); break;
        }
    }

    /// <summary>
    /// Ctrl+R: refreshes the active workspace if it supports refresh.
    /// </summary>
    [RelayCommand]
    private void WorkspaceRefresh()
    {
        switch (ShellState.Workspace)
        {
            case TaskFacadeViewModel vm:     vm.RefreshWorkspaceCommand.Execute(null); break;
            case ScheduleFacadeViewModel vm: vm.RefreshWorkspaceCommand.Execute(null); break;
            case ChronicleViewModel vm:      vm.RefreshWorkspaceCommand.Execute(null); break;
            case ZettelkastenViewModel vm:   vm.RefreshWorkspaceCommand.Execute(null); break;
        }
    }
}