using System;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;

namespace Misa.Ui.Avalonia.Infrastructure.Services.Navigation;

public class NavigationService(NavigationStore navigationStore, IClipboardService clipboardService, IServiceProvider services)
    : INavigationService
{
    public NavigationStore NavigationStore { get; } = navigationStore;
    public IClipboardService ClipboardService { get; } = clipboardService;
    public IServiceProvider ServiceProvider { get; } = services;
}