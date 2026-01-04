using System;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;
using TaskViewModel = Misa.Ui.Avalonia.Features.Tasks.TasksHub.TaskViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.GetTasks;

public class GetTaskViewModel : ViewModelBase
{
    public GetTaskViewModel(TaskViewModel vm)
    {
        MainViewModel = vm;
        _ = LoadAsync();
    }
    public TaskViewModel MainViewModel { get; }
    
    
    public async Task LoadAsync()
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