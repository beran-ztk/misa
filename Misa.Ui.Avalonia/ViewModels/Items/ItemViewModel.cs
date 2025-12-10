using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Metadata;
using ExCSS;
using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Details;
using Misa.Ui.Avalonia.ViewModels.Shells;
using Misa.Ui.Avalonia.Views.Items;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Items;

public class ItemViewModel : ViewModelBase
{
    public ItemViewModel(INavigationService navigationService, NavigationStore navigationStore)
    {
        _navigationService = navigationService;
        NavigationStore = navigationStore;
        
        _httpClient = NavigationStore.MisaHttpClient;
        ListModel = new ItemListViewModel(this, NavigationStore);
        Navigation = new ItemNavigationViewModel(this, NavigationStore);
        
        CreateDefaultEntityCommand = ReactiveCommand.CreateFromTask(CreateDefaultEntityAsync);
        CreateDefaultEntityCommand
            .ThrownExceptions
            .Subscribe(Console.WriteLine);
    }
    private CreateEntityDto? _selectedEntity;
    public NavigationStore NavigationStore { get; }
    private readonly INavigationService _navigationService;
    public CreateEntityDto? SelectedEntity
    {
        get => _selectedEntity;
        set => SetProperty(ref _selectedEntity, value);
    }
    public ItemListViewModel ListModel { get; }
    public ItemNavigationViewModel Navigation { get; }
    public DetailMainDetailViewModel DetailViewModel { get; }
    private readonly HttpClient _httpClient;

    
    public ReactiveCommand<Unit, Unit> CreateDefaultEntityCommand { get; }
    
    private async Task CreateDefaultEntityAsync()
    {
        try
        {
            var entity = new CreateEntityDto
            {
                OwnerId = null,
                WorkflowId = 1
            };
            var response = await _httpClient.PostAsJsonAsync(requestUri: "api/entities/add", entity);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Server returned {response.StatusCode}: {body}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}