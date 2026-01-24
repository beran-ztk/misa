using System;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;
using PageViewModel = Misa.Ui.Avalonia.Features.Tasks.Page.PageViewModel;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

public class NavigationService(NavigationStore navigationStore, IClipboardService clipboardService, IServiceProvider services)
    : INavigationService
{
    public NavigationStore NavigationStore { get; init; } = navigationStore;
    public IClipboardService ClipboardService { get; init; } = clipboardService;
    public IServiceProvider ServiceProvider { get; init; } = services;

    public void ShowTasks()
    {
        NavigationStore.CurrentViewModel = new PageViewModel(this);
    }
}