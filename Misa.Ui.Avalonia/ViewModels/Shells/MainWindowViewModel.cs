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

    public MainWindowViewModel(NavigationStore navigationStore, NavigationService navigationService)
    {
        _navigationStore = navigationStore;
        _navigationService = navigationService;
        
        _httpClient = _navigationStore.MisaHttpClient;

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
    }
}