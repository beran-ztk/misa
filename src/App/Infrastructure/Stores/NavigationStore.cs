using System;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.Common.Mapping;

namespace Misa.Ui.Avalonia.Infrastructure.Stores;

public partial class NavigationStore(HttpClient httpClient) : ObservableObject
{
    public HttpClient HttpClient { get; set; } = httpClient;
    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private ViewModelBase? _currentOverlay;
    [ObservableProperty] private ViewModelBase? _mainWindowOverlay;
    [ObservableProperty] private UserDto _user = new(Guid.Empty, string.Empty, string.Empty);
    public void CloseOverlay() => MainWindowOverlay = null;
    public string BreadCrumbsBase { get; } = "Misa  →  Main  →  Navigation → ";

    [ObservableProperty] private string _breadCrumbsNavigation = string.Empty;
}