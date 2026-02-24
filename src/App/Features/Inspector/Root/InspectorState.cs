using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Tasks;

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
    
    [ObservableProperty] private bool _isEditItemFormOpen;
    [ObservableProperty] private TaskCategoryDto _selectedCategory;
    public IReadOnlyList<TaskCategoryDto> Categories { get; } = Enum.GetValues<TaskCategoryDto>();
}