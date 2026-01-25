using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;

namespace Misa.Ui.Avalonia.App.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }
    public NavigationViewModel Navigation { get; }

    public ViewModelBase? CurrentViewModel => NavigationService.NavigationStore.CurrentViewModel;
    public ViewModelBase? CurrentInfoViewModel => NavigationService.NavigationStore.CurrentInfoViewModel;

    public MainWindowViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        Navigation = new NavigationViewModel(NavigationService);
        
        NavigationService.NavigationStore.PropertyChanged += (_, e) =>
        {
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CurrentInfoViewModel));
        };
    }
}