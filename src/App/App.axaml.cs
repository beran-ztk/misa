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
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Add;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Toolbar;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Toolbar;
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
        sc.AddTransient<AuthenticationWindowViewModel>();
        
        sc.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(baseAddress)
        });

        // Shell
        sc.AddSingleton<AppState>();
        sc.AddSingleton<ShellState>();
        sc.AddSingleton<UserState>();
        
        sc.AddSingleton<WorkspaceRouter>();
        
        sc.AddSingleton<AuthenticationWindowViewModel>();
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();
        sc.AddSingleton<WorkspaceNavigationViewModel>();
        sc.AddSingleton<UtilityNavigationViewModel>();
        sc.AddSingleton<FooterViewModel>();

        sc.AddSingleton<AuthenticationWindow>(sp =>
        {
            var vm = sp.GetRequiredService<AuthenticationWindowViewModel>();
            return new AuthenticationWindow { DataContext = vm };
        });
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

        // Feature - Task
        sc.AddSingleton<TaskState>();
        sc.AddSingleton<TaskFacadeViewModel>();
        sc.AddSingleton<TaskGateway>();
        sc.AddTransient<AddTaskViewModel>();
        sc.AddSingleton<TaskToolbarView>();
        sc.AddSingleton<TaskContentView>();
        
        
        sc.AddSingleton<SchedulerState>();
        sc.AddSingleton<SchedulerToolbarViewModel>();
        sc.AddSingleton<SchedulerContentViewModel>();
        sc.AddSingleton<AddScheduleViewModel>();
        
        // Utility
        sc.AddSingleton<NotificationViewModel>();
        
        Services = sc.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.MainWindow = Services.GetRequiredService<AuthenticationWindow>();
            
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