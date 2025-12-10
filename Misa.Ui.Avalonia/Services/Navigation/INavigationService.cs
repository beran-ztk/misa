using Misa.Ui.Avalonia.Stores;

namespace Misa.Ui.Avalonia.Services.Navigation;

public interface INavigationService
{
    public NavigationStore NavigationStore { get; }
    public LookupsStore LookupsStore { get; }
    public void ShowItems();
    public void ShowTasks();
    public void ShowNotifications();
}