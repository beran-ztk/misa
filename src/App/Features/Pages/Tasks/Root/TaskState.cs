using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState : ObservableObject
{
    public ShellState ShellState { get; }
    public CreateTaskState CreateState { get; }
    private ISelectionContextState SelectionContextState { get; }
    private ObservableCollection<TaskDto> Items { get; } = [];
    public ObservableCollection<TaskDto> FilteredItems { get; } = [];

    public TaskState(
        ShellState shellState,
        CreateTaskState createState,
        ISelectionContextState selectionContextState)
    {
        ShellState = shellState;
        CreateState = createState;
        SelectionContextState = selectionContextState;
        
        foreach (var p in Enum.GetValues<PriorityContract>())
        {
            var opt = new PriorityFilterOption(p, isSelected: true);
            opt.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PriorityFilterOption.IsSelected))
                    ApplyFilters();
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
    private TaskDto? _selectedItem;
    partial void OnSelectedItemChanged(TaskDto? value)
    {
        SelectionContextState.SetActive(value?.Id);
    }
    
    /// <summary>
    /// Filter
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    public ObservableCollection<PriorityFilterOption> PriorityFilters { get; } = [];
    
    private void ApplyFilters()
    {
        var activePriorities = PriorityFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Priority)
            .ToHashSet();
        
        FilteredItems.Clear();
        
        foreach (var t in Items)
        {
            if (activePriorities.Contains(t.Item.Priority) 
                && (t.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(SearchText)))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => 
                {
                    FilteredItems.Add(t);
                });
            }
        }
    }
    
    /// <summary>
    /// Adders
    /// </summary>
    public async Task AddToCollection(List<TaskDto> items)
    {
        Items.Clear();
        FilteredItems.Clear();
        
        foreach (var item in items)
        {
            await AddToCollection(item);
        }
    }
    public async Task AddToCollection(TaskDto? item)
    {
        if (item is null) return;
        
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Items.Add(item);
            FilteredItems.Add(item);
        });
    }
}