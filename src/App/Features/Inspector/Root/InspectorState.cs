using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity.Sessions;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HasItem))]
    [NotifyPropertyChangedFor(nameof(CanHaveActivity))]
    [NotifyPropertyChangedFor(nameof(IsTask))]
    private ItemDto _item = new();
    
    public bool HasItem => Item.Id != Guid.Empty;
    public bool CanHaveActivity => Item.Workflow == WorkflowDto.Task;
    public bool IsTask => Item.Workflow == WorkflowDto.Task;
    
    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
}