using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Utilities.Toast;
using Misa.Ui.Avalonia.Infrastructure.UI;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class ShellState : ObservableObject, ILayerHost, IToastHost, IWorkspaceHost
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
    
    // Panel
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    private Control? _panel;
    public bool IsPanelOpen => Panel is not null;
    
    // Modal
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsModalOpen))]
    private Control? _modal;
    public bool IsModalOpen => Modal is not null;

    
    // Side toasts — system / SignalR notifications (bottom-right)
    public ObservableCollection<ToastViewModel> Toasts { get; } = [];

    // Action toasts — transient user feedback (top-center, max 1)
    public ObservableCollection<ToastViewModel> ActionToasts { get; } = [];

    [ObservableProperty] private ViewModelBase? _busy;
}