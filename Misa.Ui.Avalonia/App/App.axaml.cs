using System;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.App.Shell;
using Misa.Ui.Avalonia.Features.Scheduler.Main;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using DetailMainWindowViewModel = Misa.Ui.Avalonia.Features.Details.Main.DetailMainWindowViewModel;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var sc = new ServiceCollection();

        // -------------------------
        // Infrastructure / Core
        // -------------------------
        sc.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("http://localhost:4500")
        });

        sc.AddSingleton<NavigationStore>();
        sc.AddSingleton<IClipboardService, ClipboardService>();
        sc.AddSingleton<INavigationService, NavigationService>();

        // -------------------------
        // Details (Selection + Clients + VMs)
        // -------------------------
        sc.AddSingleton<IActiveEntitySelection, ActiveEntitySelection>();

        sc.AddSingleton<IItemExtensionVmFactory, ItemExtensionVmFactory>();
        sc.AddTransient<IItemDetailClient, ItemDetailClient>();

        sc.AddTransient<DetailMainWindowViewModel>();
        sc.AddTransient<IDetailCoordinator, DetailCoordinator>();

        // -------------------------
        // Feature VMs
        // -------------------------
        sc.AddTransient<SchedulerMainWindowViewModel>();
        sc.AddTransient<TaskMainWindowViewModel>();

        // -------------------------
        // Shell / Window
        // -------------------------
        sc.AddTransient<MainWindowViewModel>();

        sc.AddSingleton<MainWindow>(sp =>
            new MainWindow
            {
                DataContext = sp.GetRequiredService<MainWindowViewModel>()
            });

        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }


    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}