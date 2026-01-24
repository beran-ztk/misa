using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Infrastructure.Stores;

public partial class NavigationStore(HttpClient httpClient) : ObservableObject
{
    public HttpClient MisaHttpClient { get; set; } = httpClient;
    private ViewModelBase? _currentViewModel;
    private ViewModelBase? _currentInfoViewModel;

    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }
    public ViewModelBase? CurrentInfoViewModel
    {
        get => _currentInfoViewModel;
        set => SetProperty(ref _currentInfoViewModel, value);
    }
}