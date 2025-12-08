using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using Misa.Contract.Entities;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Items;

public class ItemListViewModel : ViewModelBase
{
    public ItemListViewModel(ItemViewModel vm, NavigationStore navigationStore)
    {
        _httpClient = navigationStore.MisaHttpClient;
        MainViewModel = vm;
        _ = LoadAsync();
    }
    private readonly HttpClient _httpClient;
    public ObservableCollection<CreateEntityDto> Entities { get; set; } = [];
    public ItemViewModel MainViewModel { get; set; }
    
    private async Task LoadAsync()
    {
        try
        {
            var entities = await _httpClient
                .GetFromJsonAsync<CreateEntityDto[]>(requestUri: "api/entities/get");

            if (entities == null)
                return;
            
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Entities.Clear();
                foreach (var entity in entities)
                {
                    Entities.Add(entity);
                }
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    } 
}