using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
    
    public ViewModelBase? MainWindowOverlay => NavigationService.NavigationStore.MainWindowOverlay;
    public bool CurrentOverlayOpen => NavigationService.NavigationStore.CurrentOverlay != null;
    public bool CurrentMainOverlayOpen => NavigationService.NavigationStore.MainWindowOverlay != null;

    [RelayCommand]
    private void CloseOverlay() => NavigationService.NavigationStore.CurrentOverlay = null;

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
                case nameof(NavigationService.NavigationStore.CurrentOverlay):
                    OnPropertyChanged(nameof(CurrentOverlayOpen));
                    break;
                case nameof(NavigationService.NavigationStore.BreadCrumbsNavigation):
                    OnPropertyChanged(nameof(BreadCrumbs));
                    break;
            }
        };
    }
}