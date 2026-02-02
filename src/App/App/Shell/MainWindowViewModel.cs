using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
    public ViewModelBase? CurrentOverlay => NavigationService.NavigationStore.CurrentOverlay;
    public bool CurrentOverlayOpen => CurrentOverlay != null;

    [RelayCommand]
    private void CloseOverlay() => NavigationService.NavigationStore.CurrentOverlay = null;

    public MainWindowViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        Navigation = NavigationService.ServiceProvider.GetRequiredService<NavigationViewModel>();
        Information = NavigationService.ServiceProvider.GetRequiredService<InformationViewModel>();
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(NavigationService.NavigationStore.CurrentViewModel):
                    OnPropertyChanged(nameof(CurrentViewModel));
                    break;
                case nameof(NavigationService.NavigationStore.CurrentOverlay):
                    OnPropertyChanged(nameof(CurrentOverlay));
                    OnPropertyChanged(nameof(CurrentOverlayOpen));
                    break;
                case nameof(NavigationService.NavigationStore.BreadCrumbsNavigation):
                    OnPropertyChanged(nameof(BreadCrumbs));
                    break;
            }
        };
    }
}