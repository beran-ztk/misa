using System;
using System.Security.Cryptography.X509Certificates;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Infrastructure.States;

public partial class ShellState : ObservableObject
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
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelOpen))]
    private Control? _panel;
    public bool IsPanelOpen => Panel is not null;
    [RelayCommand] private void ClosePanel() => Modal = null;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsModalOpen))]
    private Control? _modal;
    public bool IsModalOpen => Modal is not null;
    [RelayCommand] private void CloseModal() => Modal = null;
    [ObservableProperty] private ViewModelBase? _toast;
    [ObservableProperty] private ViewModelBase? _busy;
}