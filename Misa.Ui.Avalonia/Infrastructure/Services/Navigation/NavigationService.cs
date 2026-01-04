using Misa.Ui.Avalonia.Stores;
using TaskViewModel = Misa.Ui.Avalonia.Features.Tasks.TasksHub.TaskViewModel;

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
    public void ShowTasks()
    {
        NavigationStore.CurrentViewModel = new TaskViewModel(this);
    }
}