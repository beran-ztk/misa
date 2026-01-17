using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Stores;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

public class NavigationService : INavigationService
{
    public NavigationStore NavigationStore { get; init; }
    public LookupsStore LookupsStore { get; init; }
    public IClipboardService ClipboardService { get; init; }

    public NavigationService(NavigationStore navigationStore, LookupsStore lookupsStore, IClipboardService clipboardService)
    {
        NavigationStore = navigationStore;
        LookupsStore = lookupsStore;
        ClipboardService = clipboardService;
    }
    public void ShowTasks()
    {
        NavigationStore.CurrentViewModel = new PageViewModel(this);
    }
}