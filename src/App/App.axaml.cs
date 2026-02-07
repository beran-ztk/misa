using System;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Inspector.Common;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Messaging;
using Misa.Ui.Avalonia.Infrastructure.Navigation;
using Misa.Ui.Avalonia.Infrastructure.Platform;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.Time;
using Misa.Ui.Avalonia.Shell.Authentication;
using Misa.Ui.Avalonia.Shell.Base;
using Misa.Ui.Avalonia.Shell.Components;
using InspectorViewModel = Misa.Ui.Avalonia.Features.Inspector.Base.InspectorViewModel;
using NavigationStore = Misa.Ui.Avalonia.Infrastructure.States.NavigationStore;
using NotificationViewModel = Misa.Ui.Avalonia.Features.Utilities.Notifications.NotificationViewModel;
using SchedulerMainWindowViewModel = Misa.Ui.Avalonia.Features.Pages.Scheduling.Main.SchedulerMainWindowViewModel;
using SelectionContextState = Misa.Ui.Avalonia.Infrastructure.States.SelectionContextState;
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
        sc.AddSingleton<ISelectionContextState, SelectionContextState>();

        sc.AddSingleton<IInspectorItemExtensionVmFactory, InspectorItemExtensionVmFactory>();
        sc.AddTransient<IInspectorClient, InspectorClient>();

        sc.AddTransient<InspectorViewModel>();
        sc.AddTransient<IInspectorCoordinator, InspectorCoordinator>();

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