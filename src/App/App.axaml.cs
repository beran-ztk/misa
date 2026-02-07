using System;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Messaging;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Infrastructure.Services.Startup;
using Misa.Ui.Avalonia.Shell.Authentication;
using Misa.Ui.Avalonia.Shell.Base;
using Misa.Ui.Avalonia.Shell.Components;
using DetailMainWindowViewModel = Misa.Ui.Avalonia.Features.Inspector.Main.DetailMainWindowViewModel;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.Stores.NavigationStore;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;
using SchedulerMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Scheduling.Main.SchedulerMainWindowViewModel;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia;

public class App : Application
{
    private static IServiceProvider Services { get; set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        const string baseAddress = "http://localhost:4500";
        var sc = new ServiceCollection();
        
        // -------------------------
        // Infrastructure / Core
        // -------------------------
        sc.AddSingleton<NotificationViewModel>();
        sc.AddSingleton<SignalRNotificationClient>();
        
        sc.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        });

        sc.AddSingleton<NavigationStore>();
        sc.AddSingleton<IClipboardService, ClipboardService>();
        sc.AddSingleton<INavigationService, NavigationService>();
        sc.AddSingleton<IAuthenticationService, AuthenticationService>();
        sc.AddSingleton<TimeZoneService>();

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
        sc.AddTransient<AuthenticationViewModel>();

        // -------------------------
        // Shell / Window
        // -------------------------
        sc.AddSingleton<MainWindowViewModel>();
        sc.AddSingleton<NavigationViewModel>();
        sc.AddSingleton<InformationViewModel>();

        sc.AddSingleton<MainWindow>(sp =>
        {
            var vm = sp.GetRequiredService<MainWindowViewModel>();
            return new MainWindow { DataContext = vm };
        });
        
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = Services.GetRequiredService<MainWindow>();
            
            var signal = Services.GetRequiredService<SignalRNotificationClient>();
            _ = signal.StartAsync(baseAddress);
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