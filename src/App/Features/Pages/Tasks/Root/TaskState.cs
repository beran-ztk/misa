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
    public ShellState ShellState { get; }
    public CreateTaskState CreateState { get; }
    private ISelectionContextState SelectionContextState { get; }
    private IReadOnlyCollection<TaskExtensionDto> Items { get; set; } = [];
    public ObservableCollection<TaskExtensionDto> FilteredItems { get; } = [];

    public TaskState(
        ShellState shellState,
        CreateTaskState createState,
        ISelectionContextState selectionContextState)
    {
        ShellState = shellState;
        CreateState = createState;
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
    }
    
    /// <summary>
    /// Selected Task
    /// </summary>
    public bool IsItemSelected => SelectedItem is not null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsItemSelected))]
    private TaskExtensionDto? _selectedItem;
    partial void OnSelectedItemChanged(TaskExtensionDto? value)
    {
        SelectionContextState.Set(value?.Item.Id);
    }
    
    /// <summary>
    /// Filter
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => _ = RefreshFilteredCollection();
    public ObservableCollection<PriorityFilterOption> PriorityFilters { get; } = [];
    
    private async Task RefreshFilteredCollection()
    {
        var activePriorities = PriorityFilters
            .Where(f => f.IsSelected)
            .Select(f => f.ActivityPriority)
            .ToHashSet();
        
        FilteredItems.Clear();
        
        foreach (var item in Items)
        {
            if (activePriorities.Contains(item.Activity.Priority) 
                && (item.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(SearchText)))
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
    public async Task SetMainCollection(IReadOnlyCollection<TaskExtensionDto> items)
    {
        Items = items;
        FilteredItems.Clear();
        await RefreshFilteredCollection();
    }
    public async Task AppendToMainCollection(TaskExtensionDto item)
    {
        var temp = Items.ToList();
        temp.Add(item);
        Items = temp;
        await RefreshFilteredCollection();
    }
}