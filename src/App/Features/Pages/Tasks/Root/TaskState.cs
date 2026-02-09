using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState(ISelectionContextState selectionContextState, ShellState shellState)
    : ObservableObject
{
    public ShellState ShellState { get; init; } = shellState;
    private ObservableCollection<TaskDto> Tasks { get; } = [];
    public ObservableCollection<TaskDto> FilteredTasks { get; } = [];
    
    /// <summary>
    /// Selected Task
    /// </summary>
    public bool IsTaskSelected => SelectedTask is not null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTaskSelected))]
    private TaskDto? _selectedTask;
    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        selectionContextState.SetActive(value?.Id);
    }
    
    /// <summary>
    /// Filter
    /// </summary>
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    public ObservableCollection<PriorityFilterOption> PriorityFilters { get; } = [];
    
    public void ApplyFilters()
    {
        var activePriorities = PriorityFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Priority)
            .ToHashSet();
        
        FilteredTasks.Clear();
        
        foreach (var t in Tasks)
        {
            if (activePriorities.Contains(t.Item.Priority) 
                && (t.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(SearchText)))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => 
                {
                    FilteredTasks.Add(t);
                });
            }
        }
    }
    
    /// <summary>
    /// Adders
    /// </summary>
    public async Task AddToCollection(List<TaskDto> tasks)
    {
        Tasks.Clear();
        FilteredTasks.Clear();
        
        foreach (var task in tasks)
        {
            await AddToCollection(task);
        }
    }
    public async Task AddToCollection(TaskDto task)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Tasks.Add(task);
            FilteredTasks.Add(task);
        });
    }
}