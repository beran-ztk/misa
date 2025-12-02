using System;
using System.Threading.Tasks;
using Misa.Contract.Items;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reactive;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.Stores;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Shells;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly HttpClient _httpClient;
    private readonly NavigationStore _navigationStore;
    private readonly INavigationService _navigationService;

    public ViewModelBase? CurrentViewModel => _navigationStore.CurrentViewModel;
    public ViewModelBase? CurrentInfoViewModel => _navigationStore.CurrentInfoViewModel;
    public NavigationViewModel Navigation { get; }
    public InfoBarViewModel InfoBar { get; }
    public TitleBarViewModel TitleBar { get; }
    public StatusBarViewModel StatusBar { get; }
    public ReactiveCommand<Unit, Unit> CreateItemCommand { get; }
    public string StatusText => "Meow";

    public MainWindowViewModel(NavigationStore navigationStore, NavigationService navigationService)
    {
        _navigationStore = navigationStore;
        _navigationService = navigationService;

        Navigation = new NavigationViewModel(navigationService);
        InfoBar = new InfoBarViewModel(navigationService);
        TitleBar = new TitleBarViewModel(navigationService);
        StatusBar = new StatusBarViewModel(navigationService);
        
        _navigationStore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationStore.CurrentViewModel))
            {
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        };
        
        _navigationStore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationStore.CurrentInfoViewModel))
            {
                OnPropertyChanged(nameof(CurrentInfoViewModel));
            }
        };
        
        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:4500") };
        
        CreateItemCommand = ReactiveCommand.CreateFromTask(CreateItemAsync);
        CreateItemCommand
            .ThrownExceptions
            .Subscribe(Console.WriteLine);
    }

    private async Task CreateItemAsync()
    {
        try
        {
            var item = new ItemDto("Test", "Meow");
            var response = await _httpClient.PostAsJsonAsync(requestUri: "api/items", item);
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