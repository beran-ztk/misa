using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState : ObservableObject
{
    public ISelectionContextState SelectionContextState { get; }
    private IReadOnlyCollection<TaskDto> Items { get; set; } = [];
    public ObservableCollection<TaskDto> FilteredItems { get; } = [];

    public TaskState(ISelectionContextState selectionContextState)
    {
        SelectionContextState = selectionContextState;
        
        foreach (var p in Enum.GetValues<ActivityPriorityDto>())
        {
            var opt = new PriorityFilterOption(p, isSelected: true);
            opt.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PriorityFilterOption.IsSelected))
                    _ = RefreshFilteredCollection();
            };
            PriorityFilters.Add(opt);
        }

        foreach (var s in Enum.GetValues<ActivityStateDto>())
        {
            var opt = new StateFilterOption(s, isSelected: true);
            opt.PropertyChanged += (s2, e) =>
            {
                if (e.PropertyName == nameof(StateFilterOption.IsSelected))
                    _ = RefreshFilteredCollection();
            };
            StateFilters.Add(opt);
        }

        foreach (var c in Enum.GetValues<TaskCategoryDto>())
        {
            var opt = new CategoryFilterOption(c, isSelected: true);
            opt.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CategoryFilterOption.IsSelected))
                    _ = RefreshFilteredCollection();
            };
            CategoryFilters.Add(opt);
        }
    }
    
    /// <summary>
    /// Selected Task
    /// </summary>
    public bool IsItemSelected => SelectedItem is not null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSelected))]
    private TaskDto? _selectedItem;
    partial void OnSelectedItemChanged(TaskDto? value)
    {
        SelectionContextState.Set(value?.Item.Id);
    }
    
    /// <summary>
    /// Filter
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => _ = RefreshFilteredCollection();
    public ObservableCollection<PriorityFilterOption> PriorityFilters { get; } = [];
    public ObservableCollection<StateFilterOption> StateFilters { get; } = [];
    public ObservableCollection<CategoryFilterOption> CategoryFilters { get; } = [];
    
    private async System.Threading.Tasks.Task RefreshFilteredCollection()
    {
        var activePriorities = PriorityFilters
            .Where(f => f.IsSelected)
            .Select(f => f.ActivityPriority)
            .ToHashSet();

        var activeStates = StateFilters
            .Where(f => f.IsSelected)
            .Select(f => f.ActivityState)
            .ToHashSet();

        var activeCategories = CategoryFilters
            .Where(f => f.IsSelected)
            .Select(f => f.TaskCategory)
            .ToHashSet();

        FilteredItems.Clear();

        foreach (var item in Items)
        {
            if (activePriorities.Contains(item.Activity.Priority)
                && activeStates.Contains(item.Activity.State)
                && activeCategories.Contains(item.Category)
                && (string.IsNullOrEmpty(SearchText) || item.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    FilteredItems.Add(item);
                });
            }
        }
    }
    
    /// <summary>
    /// Adders
    /// </summary>
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