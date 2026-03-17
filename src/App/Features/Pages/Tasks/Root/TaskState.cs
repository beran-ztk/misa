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

    // ── View mode ───────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCardView))]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    private TaskViewMode _viewMode = TaskViewMode.Card;

    public bool IsCardView => ViewMode == TaskViewMode.Card;
    public bool IsListView => ViewMode == TaskViewMode.List;

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

        FilteredItems.Clear();

        foreach (var item in Items)
        {
            if ((activePriorities == null || activePriorities.Contains(item.Activity.Priority))
                && (activeStates == null || activeStates.Contains(item.Activity.State))
                && (activeCategories == null || activeCategories.Contains(item.Category))
                && (string.IsNullOrEmpty(SearchText)
                    || item.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            {
                await Dispatcher.UIThread.InvokeAsync(() => FilteredItems.Add(item));
            }
        }

        RefreshMeta();
    }

    private void RefreshMeta()
    {
        var now = DateTimeOffset.UtcNow;
        var items = FilteredItems;

        var total    = items.Count;
        var open     = items.Count(t => t.Activity.State == ActivityStateDto.Open);
        var overdue  = items.Count(t => t.Activity.DueAt.HasValue && t.Activity.DueAt.Value < now);
        var dueSoon  = items.Count(t => t.Activity.DueAt.HasValue
                                        && t.Activity.DueAt.Value >= now
                                        && (t.Activity.DueAt.Value - now).TotalDays <= 3);
        var highPlus = items.Count(t => t.Activity.Priority is ActivityPriorityDto.High
                                                             or ActivityPriorityDto.Urgent
                                                             or ActivityPriorityDto.Critical);

        if (total == 0)
        {
            MetaSummary = "No tasks";
            return;
        }

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
}
