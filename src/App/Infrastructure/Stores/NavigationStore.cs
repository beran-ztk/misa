using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Infrastructure.Stores;

public partial class NavigationStore(HttpClient httpClient) : ObservableObject
{
    public HttpClient MisaHttpClient { get; set; } = httpClient;
    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private ViewModelBase? _currentOverlay;
    
    public string BreadCrumbsBase { get; } = "Misa  →  Main  →  Navigation → ";

    [ObservableProperty] private string _breadCrumbsNavigation = string.Empty;
}