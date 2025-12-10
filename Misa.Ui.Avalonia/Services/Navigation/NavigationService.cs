using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Informations;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Tasks;

namespace Misa.Ui.Avalonia.Services.Navigation;

public class NavigationService : INavigationService
{
    public NavigationStore NavigationStore { get; init; }
    public LookupsStore LookupsStore { get; init; }
    public NavigationService(NavigationStore navigationStore, LookupsStore lookupsStore)
    {
        NavigationStore = navigationStore;
        LookupsStore = lookupsStore;
    }


    public void ShowItems()
    {
        NavigationStore.CurrentViewModel = new ItemViewModel(this, NavigationStore);
    }
    public void ShowNotifications()
    {
        NavigationStore.CurrentInfoViewModel = new NotificationViewModel();
    }
    public void ShowTasks()
    {
        NavigationStore.CurrentViewModel = new TaskViewModel(this);
    }
}