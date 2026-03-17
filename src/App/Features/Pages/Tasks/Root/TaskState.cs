using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Components.FilterDropdown;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState : ObservableObject
{
    public ISelectionContextState SelectionContextState { get; }

    private IReadOnlyCollection<TaskDto> Items { get; set; } = [];
    public ObservableCollection<TaskDto> FilteredItems { get; } = [];

    // ── Filter dropdowns ────────────────────────────────────────
    public FilterDropdownViewModel PriorityFilter { get; }
    public FilterDropdownViewModel StateFilter { get; }
    public FilterDropdownViewModel CategoryFilter { get; }

    // ── Meta summary ────────────────────────────────────────────
    [ObservableProperty] private string _metaSummary = string.Empty;

    // ── Workspace mode ──────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActiveMode))]
    [NotifyPropertyChangedFor(nameof(IsArchivedMode))]
    [NotifyPropertyChangedFor(nameof(IsDeletedMode))]
    [NotifyPropertyChangedFor(nameof(IsNonActiveMode))]
    [NotifyPropertyChangedFor(nameof(CardOpacity))]
    private TaskWorkspaceMode _workspaceMode = TaskWorkspaceMode.Active;

    public bool IsActiveMode   => WorkspaceMode == TaskWorkspaceMode.Active;
    public bool IsArchivedMode => WorkspaceMode == TaskWorkspaceMode.Archived;
    public bool IsDeletedMode  => WorkspaceMode == TaskWorkspaceMode.Deleted;
    public bool IsNonActiveMode => WorkspaceMode != TaskWorkspaceMode.Active;

    // Card opacity: subtly dim archived/deleted items
    public double CardOpacity => WorkspaceMode switch
    {
        TaskWorkspaceMode.Archived => 0.72,
        TaskWorkspaceMode.Deleted  => 0.55,
        _                          => 1.0
    };

    // ── View mode ───────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private TaskViewMode _viewMode = TaskViewMode.Card;

    public bool IsCardView => ViewMode == TaskViewMode.Card;
    public bool IsListView => ViewMode == TaskViewMode.List;

    // ── Sort ────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSortByTitle))]
    [NotifyPropertyChangedFor(nameof(IsSortByState))]
    [NotifyPropertyChangedFor(nameof(IsSortByCreated))]
    [NotifyPropertyChangedFor(nameof(SortFieldLabel))]
    private TaskSortField _sortField = TaskSortField.CreatedAt;
    partial void OnSortFieldChanged(TaskSortField value) => _ = RefreshFilteredCollection();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSortAscending))]
    [NotifyPropertyChangedFor(nameof(SortDirectionGlyph))]
    private TaskSortDirection _sortDirection = TaskSortDirection.Descending;
    partial void OnSortDirectionChanged(TaskSortDirection value) => _ = RefreshFilteredCollection();

    public bool IsSortByTitle   => SortField == TaskSortField.Title;
    public bool IsSortByState   => SortField == TaskSortField.State;
    public bool IsSortByCreated => SortField == TaskSortField.CreatedAt;
    public bool IsSortAscending => SortDirection == TaskSortDirection.Ascending;

    public string SortFieldLabel => SortField switch
    {
        TaskSortField.Title    => "Title",
        TaskSortField.State    => "State",
        TaskSortField.CreatedAt => "Created",
        _                      => "?"
    };

    /// <summary>▲ for ascending, ▼ for descending — used as a compact direction glyph in the toolbar.</summary>
    public string SortDirectionGlyph => SortDirection == TaskSortDirection.Ascending ? "▲" : "▼";

    // ── Search ──────────────────────────────────────────────────
    [ObservableProperty]
    private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => _ = RefreshFilteredCollection();

    // ── Selection ───────────────────────────────────────────────
    [ObservableProperty]
    private TaskDto? _selectedItem;
    partial void OnSelectedItemChanged(TaskDto? value)
    {
        SelectionContextState.Set(value?.Item.Id);
    }

    public TaskState(ISelectionContextState selectionContextState)
    {
        SelectionContextState = selectionContextState;

        PriorityFilter = new FilterDropdownViewModel(
            "Priority",
            Enum.GetValues<ActivityPriorityDto>().Select(p => p.ToString()));

        StateFilter = new FilterDropdownViewModel(
            "State",
            Enum.GetValues<ActivityStateDto>().Select(s => s.ToString()));

        CategoryFilter = new FilterDropdownViewModel(
            "Category",
            Enum.GetValues<TaskCategoryDto>().Select(c => c.ToString()));

        PriorityFilter.FilterChanged += (_, _) => _ = RefreshFilteredCollection();
        StateFilter.FilterChanged += (_, _) => _ = RefreshFilteredCollection();
        CategoryFilter.FilterChanged += (_, _) => _ = RefreshFilteredCollection();
    }

    private async Task RefreshFilteredCollection()
    {
        // No selection = inactive = pass all; selection = restrict to selected values.
        var activePriorities = PriorityFilter.HasActiveSelection
            ? PriorityFilter.SelectedLabels.Select(Enum.Parse<ActivityPriorityDto>).ToHashSet()
            : null;

        var activeStates = StateFilter.HasActiveSelection
            ? StateFilter.SelectedLabels.Select(Enum.Parse<ActivityStateDto>).ToHashSet()
            : null;

        var activeCategories = CategoryFilter.HasActiveSelection
            ? CategoryFilter.SelectedLabels.Select(Enum.Parse<TaskCategoryDto>).ToHashSet()
            : null;

        // 1. Filter
        var filtered = Items.Where(item =>
            (activePriorities == null || activePriorities.Contains(item.Activity.Priority))
            && (activeStates == null || activeStates.Contains(item.Activity.State))
            && (activeCategories == null || activeCategories.Contains(item.Category))
            && (string.IsNullOrEmpty(SearchText)
                || item.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        // 2. Sort
        IEnumerable<TaskDto> sorted = SortField switch
        {
            TaskSortField.Title => SortDirection == TaskSortDirection.Ascending
                ? filtered.OrderBy(t => t.Item.Title, StringComparer.OrdinalIgnoreCase)
                : filtered.OrderByDescending(t => t.Item.Title, StringComparer.OrdinalIgnoreCase),

            TaskSortField.State => SortDirection == TaskSortDirection.Ascending
                ? filtered.OrderBy(t => t.Activity.State)
                : filtered.OrderByDescending(t => t.Activity.State),

            TaskSortField.CreatedAt => SortDirection == TaskSortDirection.Ascending
                ? filtered.OrderBy(t => t.Item.CreatedAt)
                : filtered.OrderByDescending(t => t.Item.CreatedAt),

            _ => filtered
        };

        // 3. Populate
        FilteredItems.Clear();
        foreach (var item in sorted)
            await Dispatcher.UIThread.InvokeAsync(() => FilteredItems.Add(item));

        RefreshMeta();
    }

    private void RefreshMeta()
    {
        var items = FilteredItems;
        var total  = items.Count;

        if (total == 0)
        {
            MetaSummary = WorkspaceMode switch
            {
                TaskWorkspaceMode.Archived => "No archived tasks",
                TaskWorkspaceMode.Deleted  => "No deleted tasks",
                _                          => "No tasks"
            };
            return;
        }

        if (WorkspaceMode == TaskWorkspaceMode.Archived)
        {
            MetaSummary = $"{total} archived task{(total == 1 ? "" : "s")}";
            return;
        }

        if (WorkspaceMode == TaskWorkspaceMode.Deleted)
        {
            MetaSummary = $"{total} deleted task{(total == 1 ? "" : "s")}";
            return;
        }

        // Active mode — full stats
        var now      = DateTimeOffset.UtcNow;
        var open     = items.Count(t => t.Activity.State == ActivityStateDto.Open);
        var overdue  = items.Count(t => t.Activity.DueAt.HasValue && t.Activity.DueAt.Value < now);
        var dueSoon  = items.Count(t => t.Activity.DueAt.HasValue
                                        && t.Activity.DueAt.Value >= now
                                        && (t.Activity.DueAt.Value - now).TotalDays <= 3);
        var highPlus = items.Count(t => t.Activity.Priority is ActivityPriorityDto.High
                                                             or ActivityPriorityDto.Urgent
                                                             or ActivityPriorityDto.Critical);

        var parts = new System.Text.StringBuilder();
        parts.Append($"{total} task{(total == 1 ? "" : "s")}");
        parts.Append($"  ·  {open} open");
        if (dueSoon  > 0) parts.Append($"  ·  {dueSoon} due soon");
        if (overdue  > 0) parts.Append($"  ·  {overdue} overdue");
        if (highPlus > 0) parts.Append($"  ·  {highPlus} high+");

        MetaSummary = parts.ToString();
    }

    public async Task SetMainCollection(IReadOnlyCollection<TaskDto> items)
    {
        Items = items;
        FilteredItems.Clear();
        await RefreshFilteredCollection();
    }

    public async Task AppendToMainCollection(TaskDto item)
    {
        var temp = Items.ToList();
        temp.Add(item);
        Items = temp;
        await RefreshFilteredCollection();
    }

    public void RemoveFromMainCollection(TaskDto item)
    {
        var temp = Items.ToList();
        temp.Remove(item);
        Items = temp;
        FilteredItems.Remove(item);

        if (SelectedItem?.Item.Id == item.Item.Id)
            SelectedItem = null;

        RefreshMeta();
    }
}
