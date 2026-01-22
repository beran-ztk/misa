using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Stores;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

public interface INavigationService
{
    public NavigationStore NavigationStore { get; }
    public IClipboardService ClipboardService { get; }
    public void ShowTasks();
}