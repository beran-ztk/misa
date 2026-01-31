using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Features.Tasks.Content;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;
using DetailMainWindowViewModel = Misa.Ui.Avalonia.Features.Details.Main.DetailMainWindowViewModel;
using TaskHeaderViewModel = Misa.Ui.Avalonia.Features.Tasks.Header.TaskHeaderViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Main;

public partial class TaskMainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly IActiveEntitySelection _selection;
    public Window? HostWindow { get; set; }
    public ObservableCollection<TaskDto> Tasks { get; } = [];
    
    [ObservableProperty] private TaskDto? _selectedTask;
    [ObservableProperty] private ViewModelBase? _infoView;
    public INavigationService NavigationService { get; }
    
    private DetailMainWindowViewModel? _detailVm;
    private IDetailCoordinator? _detailCoordinator;
    
    public TaskContentViewModel Model { get; }
    public TaskHeaderViewModel TaskHeader { get; }
    [ObservableProperty] private string? _pageError;
    public TaskMainWindowViewModel(
        IServiceProvider sp,
        IActiveEntitySelection selection,
        INavigationService navigationService)
    {
        _sp = sp;
        _selection = selection;
        NavigationService = navigationService;
        
        Model = new TaskContentViewModel(this);
        TaskHeader = new TaskHeaderViewModel(this);
    }
    private void EnsureDetail()
    {
        if (_detailVm is not null) return;

        _detailVm = ActivatorUtilities.CreateInstance<DetailMainWindowViewModel>(_sp);
        
        _detailCoordinator = ActivatorUtilities.CreateInstance<DetailCoordinator>(_sp, _detailVm);
        _ = _detailCoordinator.ActivateAsync();
    }
    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        EnsureDetail();
        
        _selection.SetActive(value?.Id);
        InfoView = _detailVm;
    }

    public async Task AddToCollection(List<TaskDto> tasks)
    {
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
        });
    }
    public async Task CreateTask(AddTaskDto dto)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "tasks");
            request.Content = JsonContent.Create(dto);
            
            using var response = await NavigationService.NavigationStore.MisaHttpClient
                .SendAsync(request, CancellationToken.None);
    
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
}
