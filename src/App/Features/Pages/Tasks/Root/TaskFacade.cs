using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacade
{
    public TaskState TaskState { get; init; }
    public HttpClient HttpClient { get; init; }
    
    public TaskFacade(TaskState taskState, HttpClient httpClient)
    {
        TaskState = taskState;
        HttpClient = httpClient;
        
        foreach (var p in Enum.GetValues<PriorityContract>())
        {
            var opt = new PriorityFilterOption(p, isSelected: true);
            opt.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PriorityFilterOption.IsSelected))
                    TaskState.ApplyFilters();
            };
            TaskState.PriorityFilters.Add(opt);
        }
    }
    
    [RelayCommand]
    private void RefreshTaskWindow()
    {
        TaskState.SelectedTask = null;
        _ = LoadAsync();
    }

    [RelayCommand]
    private void ShowAddTask()
    {
        var vm = new AddTaskViewModel();

        vm.Completed += async dto =>
        {
            await CreateTask(dto);

                TaskState.ShellState.Panel = null;
        };

        vm.Cancelled += () =>
        {
            TaskState.ShellState.Panel = null;
        };

        TaskState.ShellState.Panel = vm; 
    }
    
    public async Task LoadAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "tasks");
            
            using var response = await HttpClient.SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<List<TaskDto>>>(cancellationToken: CancellationToken.None);
        
            await TaskState.AddToCollection(result?.Value ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
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
            
            using var response = await HttpClient.SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            var createdTask = await response.Content.ReadFromJsonAsync<Result<TaskDto>>(CancellationToken.None);
            if (createdTask?.Value != null)
            {
                await TaskState.AddToCollection(createdTask.Value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}