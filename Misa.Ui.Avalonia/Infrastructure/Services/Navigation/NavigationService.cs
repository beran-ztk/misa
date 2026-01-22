using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Stores;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

public class NavigationService(NavigationStore navigationStore, IClipboardService clipboardService)
    : INavigationService
{
    public NavigationStore NavigationStore { get; init; } = navigationStore;
    public IClipboardService ClipboardService { get; init; } = clipboardService;

    public void ShowTasks()
    {
        NavigationStore.CurrentViewModel = new PageViewModel(this);
    }
}