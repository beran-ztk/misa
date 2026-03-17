using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Items.Components.Schedules;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorState : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEntryTab))]
    [NotifyPropertyChangedFor(nameof(IsActivityTab))]
    private int _selectedTabIndex;

    public bool IsEntryTab => SelectedTabIndex == 0;
    public bool IsActivityTab => SelectedTabIndex == 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItem))]
    [NotifyPropertyChangedFor(nameof(HasActivityTab))]
    [NotifyPropertyChangedFor(nameof(IsRealActivity))]
    [NotifyPropertyChangedFor(nameof(IsTask))]
    [NotifyPropertyChangedFor(nameof(IsScheduler))]
    [NotifyPropertyChangedFor(nameof(HasExtension))]
    private ItemDto _item = new();

    public bool HasItem => Item.Id != Guid.Empty;
    /// <summary>Whether the Activity tab should be visible. True for Task and Schedule.</summary>
    public bool HasActivityTab => Item.Workflow is WorkflowDto.Task or WorkflowDto.Schedule;
    /// <summary>Whether the item has a real ItemActivity (State, Priority, Deadline, Sessions). True for Task only.</summary>
    public bool IsRealActivity => Item.Workflow == WorkflowDto.Task;
    public bool IsTask => Item.Workflow == WorkflowDto.Task;
    public bool IsScheduler => Item.Workflow == WorkflowDto.Schedule;
    public bool HasExtension => Item.Workflow is WorkflowDto.Task or WorkflowDto.Schedule;
    
    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
    
    [ObservableProperty] private bool _isEditItemFormOpen;
    [ObservableProperty] private TaskCategoryDto _selectedCategory;
    public IReadOnlyList<TaskCategoryDto> TaskCategories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<ActivityStateDto> ActivityStates { get; } =
        Enum.GetValues<ActivityStateDto>().Where(s => s != ActivityStateDto.Expired).ToList();
    public IReadOnlyList<ActivityPriorityDto> ActivityPriorities { get; } = Enum.GetValues<ActivityPriorityDto>();
    public IReadOnlyList<ScheduleMisfirePolicyDto> MisfirePolicies { get; } = Enum.GetValues<ScheduleMisfirePolicyDto>();
}