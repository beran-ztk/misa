using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskState(ISelectionContextState selectionContextState, HttpClient httpClient)
    : ObservableObject
{
    /// <summary>
    /// Task Collections
    /// </summary>
    public ObservableCollection<TaskDto> Tasks { get; } = [];
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
                _ = AddToFilteredCollection(t);
            }
        }
    }

    /// <summary>
    /// Adders
    /// </summary>
    private async Task AddToCollection(List<TaskDto> tasks)
    {
        Tasks.Clear();
        FilteredTasks.Clear();
        
        foreach (var task in tasks)
        {
            await AddToCollection(task);
        }
    }

    private async Task AddToCollection(TaskDto task)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Tasks.Add(task);
            FilteredTasks.Add(task);
        });
    }

    private async Task AddToFilteredCollection(TaskDto task)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            FilteredTasks.Add(task);
        });
    }
    /// <summary>
    /// Create Task Command
    /// </summary>
    public async Task CreateTask(AddTaskDto dto)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "tasks");
            request.Content = JsonContent.Create(dto);
            
            using var response = await httpClient.SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            var createdTask = await response.Content.ReadFromJsonAsync<Result<TaskDto>>(CancellationToken.None);
            if (createdTask?.Value != null)
            {
                await AddToCollection(createdTask.Value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    /// <summary>
    /// Load Tasks
    /// </summary>
    public async Task LoadAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "tasks");
            
            using var response = await httpClient.SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<List<TaskDto>>>(cancellationToken: CancellationToken.None);
        
            await AddToCollection(result?.Value ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}