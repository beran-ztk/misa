using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Tasks;

public class TaskListViewModel : ViewModelBase
{
    public TaskListViewModel(TaskViewModel vm, NavigationStore navigationStore)
    {
        _httpClient = navigationStore.MisaHttpClient;
        MainViewModel = vm;
        _ = LoadAsync();
    }
    private readonly HttpClient _httpClient;
    public TaskViewModel MainViewModel { get; }
    
    
    private async Task LoadAsync()
    {
        try
        {
            var items = await _httpClient
                .GetFromJsonAsync<ReadItemDto[]>(requestUri: "api/tasks");

            if (items == null)
                return;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainViewModel.Items.Clear();
                foreach (var item in items)
                {
                    MainViewModel.Items.Add(item);
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    } 
}