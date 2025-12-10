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
    public TaskListViewModel(TaskViewModel vm)
    {
        MainViewModel = vm;
        _ = LoadAsync();
    }
    public TaskViewModel MainViewModel { get; }
    
    
    private async Task LoadAsync()
    {
        try
        {
            var response = await MainViewModel.NavigationService.NavigationStore.MisaHttpClient
                .GetFromJsonAsync<ReadItemDto[]>(requestUri: "api/tasks");
            
            if (response == null)
                return;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainViewModel.Items.Clear();
                foreach (var item in response)
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