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
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState : ObservableObject
{
    public ShellState ShellState { get; }
    public CreateTaskState CreateTaskState { get; }
    private ISelectionContextState SelectionContextState { get; }
    private ObservableCollection<TaskDto> Tasks { get; } = [];
    public ObservableCollection<TaskDto> FilteredTasks { get; } = [];

    public TaskState(
        ShellState shellState,
        CreateTaskState createTaskState,
        ISelectionContextState selectionContextState)
    {
        ShellState = shellState;
        CreateTaskState = createTaskState;
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
    public bool IsTaskSelected => SelectedTask is not null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTaskSelected))]
    private TaskDto? _selectedTask;
    partial void OnSelectedTaskChanged(TaskDto? value)
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
    public async Task AddToCollection(TaskDto? task)
    {
        if (task is null) return;
        
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Tasks.Add(task);
            FilteredTasks.Add(task);
        });
    }
}