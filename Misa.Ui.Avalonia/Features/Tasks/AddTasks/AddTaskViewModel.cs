using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Lookups;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;
using TaskViewModel = Misa.Ui.Avalonia.Features.Tasks.TasksHub.TaskViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.AddTasks;

public partial class AddTaskViewModel : ViewModelBase
{
    public TaskViewModel MainViewModel { get; }
    public ReactiveCommand<Unit, Unit> CreateTaskCommand { get; }
    
    private string _title = string.Empty;
    private int _priorityId = 1;
    private int _categoryId = 1;
    private string? _errorMessageTitle = null;
    private IBrush? _titleBorderBrush;
    public AddTaskViewModel(TaskViewModel vm)
    {
        MainViewModel = vm;
        
        CreateTaskCommand = ReactiveCommand.CreateFromTask
        (
            execute: CreateTaskCommandAsync,
            canExecute: this.WhenAnyValue(x => x.Title,
                    title => !string.IsNullOrWhiteSpace(title))
        );
        CreateTaskCommand
            .ThrownExceptions
            .Subscribe(Console.WriteLine);
    }
    
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    public int PriorityId
    {
        get => _priorityId;
        set => SetProperty(ref _priorityId, value);
    }
    public int CategoryId
    {
        get => _categoryId;
        set => SetProperty(ref _categoryId, value);
    }
    public string? ErrorMessageTitle
    {
        get => _errorMessageTitle;
        set => SetProperty(ref _errorMessageTitle, value);
    }
    public IBrush? TitleBorderBrush
    {
        get => _titleBorderBrush;
        set => SetProperty(ref _titleBorderBrush, value);
    }
    public IReadOnlyList<PriorityDto> Priorities =>
        MainViewModel.NavigationService.LookupsStore.Priorities;
    public IReadOnlyList<CategoryDto> Categories =>
        MainViewModel.NavigationService.LookupsStore.TaskCategories;

    public void TitleError()
    {
        ErrorMessageTitle = "Please specify a title";
        TitleBorderBrush = Brushes.Red;
    }

    [RelayCommand]
    private void CancelTask()
    {
        MainViewModel.IsCreateTaskFormOpen = false;
        MainViewModel.CurrentInfoModel = null;
    }
    private async Task CreateTaskCommandAsync()
    {
        try
        {
            var trimmedTitle = Title?.Trim();

            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                TitleError();
                return;
            }
            
            var dto = new CreateItemDto
            {
                OwnerId    = null,
                PriorityId = PriorityId,
                CategoryId = CategoryId,
                Title      = trimmedTitle
            };
            
            var response = await MainViewModel.NavigationService.NavigationStore
                .MisaHttpClient.PostAsJsonAsync(requestUri: "api/tasks", dto);
            var createdItem = await response.Content.ReadFromJsonAsync<ReadItemDto>();
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Server returned {response.StatusCode}: {body}");
            }
            else
            {
                if (createdItem != null)
                    MainViewModel.Items.Add(createdItem);
                MainViewModel.IsCreateTaskFormOpen = false;
                MainViewModel.SelectedTask = createdItem;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}