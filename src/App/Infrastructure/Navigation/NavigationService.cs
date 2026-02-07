using System;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.States.NavigationStore;

namespace Misa.Ui.Avalonia.Infrastructure.Navigation;
public interface INavigationService
{
    public NavigationStore NavigationStore { get; }
    public IServiceProvider ServiceProvider { get; }
}
public class NavigationService(NavigationStore navigationStore, IServiceProvider services)
    : INavigationService
{
    public NavigationStore NavigationStore { get; } = navigationStore;
    public IServiceProvider ServiceProvider { get; } = services;
}