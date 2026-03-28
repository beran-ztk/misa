using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Items.Components.Tasks;

namespace Misa.Ui.Avalonia.Features.Inspector;
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
    private Item _item;

    public bool HasItem => Item.Id.Value != Guid.Empty;
    /// <summary>Whether the Activity tab should be visible. True for Task and Schedule.</summary>
    public bool HasActivityTab => Item.Workflow is Workflow.Task or Workflow.Schedule;
    /// <summary>Whether the item has a real ItemActivity (State, Priority, Deadline, Sessions). True for Task only.</summary>
    public bool IsRealActivity => Item.Workflow == Workflow.Task;
    public bool IsTask => Item.Workflow == Workflow.Task;
    public bool IsScheduler => Item.Workflow == Workflow.Schedule;
    public bool HasExtension => Item.Workflow is Workflow.Task or Workflow.Schedule;

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
        Workflow.Task     => "Task",
        Workflow.Schedule => "Schedule",
        _                    => "Item"
    };

    /// <summary>Non-null when the item is archived or deleted; explains why actions are restricted.</summary>
    public string? LifecycleStatusMessage =>
        Item.IsDeleted  ? $"{ItemTypeName} is deleted. Actions are disabled until it is restored." :
        Item.IsArchived ? $"{ItemTypeName} is archived. Some actions are disabled until it is restored." :
        null;
    
    // ── Terminal task state detection ─────────────────────────────────────
    public bool IsDoneState             => IsTask && Item.Activity?.State == ActivityState.Done;
    public bool IsFailedOrCanceledState => IsTask && Item.Activity?.State is ActivityState.Failed or ActivityState.Canceled;
    public bool IsExpiredState          => IsTask && Item.Activity?.State == ActivityState.Expired;
    public bool IsTerminalState         => IsDoneState || IsFailedOrCanceledState || IsExpiredState;
    public string ErrorStateBadgeLabel  => Item.Activity?.State == ActivityState.Failed ? "FAILED" : "CANCELED";

    [ObservableProperty] private CurrentSessionOverviewDto? _currentSessionOverview;
    
    [ObservableProperty] private bool _isEditItemFormOpen;
    [ObservableProperty] private TaskCategory _selectedCategory;
    public IReadOnlyList<TaskCategory> TaskCategories { get; } = Enum.GetValues<TaskCategory>();
    public IReadOnlyList<ActivityState> ActivityStates { get; } =
        Enum.GetValues<ActivityState>().Where(s => s != ActivityState.Expired).ToList();
    public IReadOnlyList<ActivityPriority> ActivityPriorities { get; } = Enum.GetValues<ActivityPriority>();
    public IReadOnlyList<ScheduleMisfirePolicy> MisfirePolicies { get; } = Enum.GetValues<ScheduleMisfirePolicy>();
}