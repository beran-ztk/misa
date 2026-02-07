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
using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Header;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Composition;
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
        sc.AddSingleton<SignalRNotificationClient>();
        sc.AddTransient<AuthenticationViewModel>();
        
        sc.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        });

        // Shell
        sc.AddSingleton<AppState>();
        sc.AddSingleton<ShellState>();
        sc.AddSingleton<WorkspaceState>();
        
        sc.AddSingleton<WorkspaceRouter>();
        
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();
        sc.AddSingleton<WorkspaceNavigationViewModel>();
        sc.AddSingleton<WorkspaceViewModel>();
        sc.AddSingleton<UtilityNavigationViewModel>();
        sc.AddSingleton<FooterViewModel>();

        sc.AddSingleton<ShellWindow>(sp =>
        {
            var vm = sp.GetRequiredService<ShellWindowViewModel>();
            return new ShellWindow { DataContext = vm };
        });
        
        // Infrastructure
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

        // Features
        sc.AddSingleton<TaskState>();
        sc.AddSingleton<TaskCoordinator>();
        sc.AddSingleton<TaskHeaderViewModel>();
        sc.AddSingleton<TaskContentViewModel>();
        
        sc.AddSingleton<SchedulerMainWindowViewModel>();
        
        // Utility
        sc.AddSingleton<NotificationViewModel>();
        
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = Services.GetRequiredService<ShellWindow>();
            
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