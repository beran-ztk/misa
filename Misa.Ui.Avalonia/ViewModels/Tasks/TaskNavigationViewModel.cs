using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Shells;
using Misa.Ui.Avalonia.Views.Tasks;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Tasks;

public class TaskNavigationViewModel : ViewModelBase
{
    public TaskNavigationViewModel(TaskViewModel vm, NavigationStore navigationStore)
    {
        MainViewModel = vm;
        _httpClient = navigationStore.MisaHttpClient;
        AddTaskCommand = ReactiveCommand.CreateFromTask(AddTaskCommandAsync);
        AddTaskCommand
            .ThrownExceptions
            .Subscribe(Console.WriteLine);
    }
    public TaskViewModel MainViewModel { get; }
    private readonly HttpClient _httpClient;
    public ReactiveCommand<Unit, Unit> AddTaskCommand { get; }
    
    private async Task AddTaskCommandAsync()
    {
        MainViewModel.CurrentInfoModel = new TaskCreateViewModel();
        return;
        try
        {
            var dto = new CreateItemDto
            {
                OwnerId = null,
                StateId = 1,
                PriorityId = 1,
                CategoryId = 1,
                Title = "Meow"
            };
            var response = await _httpClient.PostAsJsonAsync(requestUri: "api/tasks", dto);
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
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}