using System;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.Stores;

public partial class NavigationStore : ObservableObject
{
    public NavigationStore(HttpClient httpClient)
    {
        MisaHttpClient = httpClient;
    }

    public HttpClient MisaHttpClient { get; set; }
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