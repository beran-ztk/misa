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
    private readonly LookupsStore _lookupsStore;
    public INavigationService NavigationService { get; set; }

    public ViewModelBase? CurrentViewModel => NavigationService.NavigationStore.CurrentViewModel;
    public ViewModelBase? CurrentInfoViewModel => NavigationService.NavigationStore.CurrentInfoViewModel;
    public NavigationViewModel Navigation { get; }
    public InfoBarViewModel InfoBar { get; }
    public TitleBarViewModel TitleBar { get; }
    public StatusBarViewModel StatusBar { get; }

    public MainWindowViewModel(INavigationService navigationService, LookupsStore lookupsStore)
    {
        NavigationService = navigationService;
        _lookupsStore = lookupsStore;
        
        _httpClient = NavigationService.NavigationStore.MisaHttpClient;

        Navigation = new NavigationViewModel(NavigationService);
        InfoBar = new InfoBarViewModel(NavigationService);
        TitleBar = new TitleBarViewModel(NavigationService);
        StatusBar = new StatusBarViewModel(NavigationService);
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationStore.CurrentViewModel))
            {
                OnPropertyChanged(nameof(CurrentViewModel));
            }
        };
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationStore.CurrentInfoViewModel))
            {
                OnPropertyChanged(nameof(CurrentInfoViewModel));
            }
        };
    }
}