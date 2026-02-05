using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Features.Authentication;
using Misa.Ui.Avalonia.App.Authentication;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }
    public NavigationViewModel Navigation { get; }
    public InformationViewModel Information { get; }
    public string Version => "v1.0.0";
    public string BreadCrumbs => NavigationService.NavigationStore.BreadCrumbsBase + NavigationService.NavigationStore.BreadCrumbsNavigation;
    
    public ViewModelBase? CurrentViewModel => NavigationService.NavigationStore.CurrentViewModel;

    public bool CurrentOverlayOpen => NavigationService.NavigationStore.CurrentOverlay != null;
    
    public ViewModelBase? MainWindowOverlay => NavigationService.NavigationStore.MainWindowOverlay;
    public bool CurrentMainOverlayOpen => MainWindowOverlay != null;

    [RelayCommand]
    private void CloseOverlay() => NavigationService.NavigationStore.CurrentOverlay = null;
    public UserDto User => NavigationService.NavigationStore.User;

    public MainWindowViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        Navigation = NavigationService.ServiceProvider.GetRequiredService<NavigationViewModel>();
        Information = NavigationService.ServiceProvider.GetRequiredService<InformationViewModel>();

        NavigationService.NavigationStore.MainWindowOverlay =
            NavigationService.ServiceProvider.GetRequiredService<AuthenticationViewModel>();
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(NavigationService.NavigationStore.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    break;
                case nameof(NavigationService.NavigationStore.CurrentOverlay):
                    OnPropertyChanged(nameof(CurrentOverlayOpen));
                    break;
                case nameof(NavigationService.NavigationStore.User):
                    OnPropertyChanged(nameof(User));
                    break;
                case nameof(NavigationService.NavigationStore.MainWindowOverlay):
                    OnPropertyChanged(nameof(MainWindowOverlay));
                    OnPropertyChanged(nameof(CurrentMainOverlayOpen));
                    break;
                case nameof(NavigationService.NavigationStore.BreadCrumbsNavigation):
                    OnPropertyChanged(nameof(BreadCrumbs));
                    break;
            }
        };
    }
    [ObservableProperty]
    private bool _isUserMenuOpen;
    [RelayCommand]
    private void ToggleUserMenu()
    {
        IsUserMenuOpen = !IsUserMenuOpen;
    }
    [RelayCommand]
    private void CloseUserMenu()
    {
        IsUserMenuOpen = false;
    }
    [RelayCommand]
    private void SignOut()
    {
        NavigationService.NavigationStore.MainWindowOverlay = NavigationService.ServiceProvider.GetRequiredService<AuthenticationViewModel>();
    }
}