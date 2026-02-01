using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.App.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }
    public NavigationViewModel Navigation { get; }
    public string Version { get; } = "v1.0.0";
    public string BreadCrumbs => NavigationService.NavigationStore.BreadCrumbsBase + NavigationService.NavigationStore.BreadCrumbsNavigation;

    
    public ViewModelBase? CurrentViewModel => NavigationService.NavigationStore.CurrentViewModel;

    public MainWindowViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        Navigation = new NavigationViewModel(NavigationService);
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(BreadCrumbs));
        };
    }
}