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
    [NotifyPropertyChangedFor(nameof(IsArchived))]
    [NotifyPropertyChangedFor(nameof(IsDeleted))]
    [NotifyPropertyChangedFor(nameof(IsReadOnlyDueToLifecycle))]
    [NotifyPropertyChangedFor(nameof(HasLifecycleRestriction))]
    [NotifyPropertyChangedFor(nameof(CanArchive))]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    [NotifyPropertyChangedFor(nameof(CanManageSessions))]
    [NotifyPropertyChangedFor(nameof(LifecycleStatusMessage))]
    [NotifyPropertyChangedFor(nameof(IsDoneState))]
    [NotifyPropertyChangedFor(nameof(IsFailedOrCanceledState))]
    [NotifyPropertyChangedFor(nameof(IsExpiredState))]
    [NotifyPropertyChangedFor(nameof(IsTerminalState))]
    [NotifyPropertyChangedFor(nameof(ErrorStateBadgeLabel))]
    private ItemDto _item = new();

    public bool HasItem => Item.Id != Guid.Empty;
    /// <summary>Whether the Activity tab should be visible. True for Task and Schedule.</summary>
    public bool HasActivityTab => Item.Workflow is WorkflowDto.Task or WorkflowDto.Schedule;
    /// <summary>Whether the item has a real ItemActivity (State, Priority, Deadline, Sessions). True for Task only.</summary>
    public bool IsRealActivity => Item.Workflow == WorkflowDto.Task;
    public bool IsTask => Item.Workflow == WorkflowDto.Task;
    public bool IsScheduler => Item.Workflow == WorkflowDto.Schedule;
    public bool HasExtension => Item.Workflow is WorkflowDto.Task or WorkflowDto.Schedule;

    // ── Lifecycle action availability ────────────────────────────────────
    public bool IsArchived               => Item.IsArchived;
    public bool IsDeleted                => Item.IsDeleted;
    public bool IsReadOnlyDueToLifecycle => Item.IsArchived || Item.IsDeleted;
    public bool HasLifecycleRestriction  => IsReadOnlyDueToLifecycle;

    /// <summary>Can the item be archived. False when already archived or deleted.</summary>
    public bool CanArchive        => !Item.IsArchived && !Item.IsDeleted;
    /// <summary>Can the item be soft-deleted. False when already deleted.</summary>
    public bool CanDelete         => !Item.IsDeleted;
    /// <summary>Can item fields be edited. False for archived or deleted items.</summary>
    public bool CanEdit           => !Item.IsArchived && !Item.IsDeleted;
    /// <summary>Can sessions be started/paused/ended. False for archived or deleted items.</summary>
    public bool CanManageSessions => !Item.IsArchived && !Item.IsDeleted;

    private string ItemTypeName => Item.Workflow switch
    {
        WorkflowDto.Task     => "Task",
        WorkflowDto.Schedule => "Schedule",
        _                    => "Item"
    };

    /// <summary>Non-null when the item is archived or deleted; explains why actions are restricted.</summary>
    public string? LifecycleStatusMessage =>
        Item.IsDeleted  ? $"{ItemTypeName} is deleted. Actions are disabled until it is restored." :
        Item.IsArchived ? $"{ItemTypeName} is archived. Some actions are disabled until it is restored." :
        null;
    
    // ── Terminal task state detection ─────────────────────────────────────
    public bool IsDoneState             => IsTask && Item.Activity?.State == ActivityStateDto.Done;
    public bool IsFailedOrCanceledState => IsTask && Item.Activity?.State is ActivityStateDto.Failed or ActivityStateDto.Canceled;
    public bool IsExpiredState          => IsTask && Item.Activity?.State == ActivityStateDto.Expired;
    public bool IsTerminalState         => IsDoneState || IsFailedOrCanceledState || IsExpiredState;
    public string ErrorStateBadgeLabel  => Item.Activity?.State == ActivityStateDto.Failed ? "FAILED" : "CANCELED";

    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
    
    [ObservableProperty] private bool _isEditItemFormOpen;
    [ObservableProperty] private TaskCategoryDto _selectedCategory;
    public IReadOnlyList<TaskCategoryDto> TaskCategories { get; } = Enum.GetValues<TaskCategoryDto>();
    public IReadOnlyList<ActivityStateDto> ActivityStates { get; } =
        Enum.GetValues<ActivityStateDto>().Where(s => s != ActivityStateDto.Expired).ToList();
    public IReadOnlyList<ActivityPriorityDto> ActivityPriorities { get; } = Enum.GetValues<ActivityPriorityDto>();
    public IReadOnlyList<ScheduleMisfirePolicyDto> MisfirePolicies { get; } = Enum.GetValues<ScheduleMisfirePolicyDto>();
}