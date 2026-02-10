using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Toolbar;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Content;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Toolbar;
using Misa.Ui.Avalonia.Features.Utilities.Notifications;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.Messaging;
using Misa.Ui.Avalonia.Infrastructure.Platform;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.Time;
using Misa.Ui.Avalonia.Infrastructure.UI;
using Misa.Ui.Avalonia.Shell.Authentication;
using Misa.Ui.Avalonia.Shell.Base;
using Misa.Ui.Avalonia.Shell.Components;

namespace Misa.Ui.Avalonia.Infrastructure.Composition;

public static class CompositionRoot
{
    public static IServiceProvider Build(string baseAddress)
    {
        var sc = new ServiceCollection();

        sc.AddCore(baseAddress);
        sc.AddShell();
        sc.AddInfrastructure();
        sc.AddInspector();
        sc.AddTasksFeature();
        sc.AddSchedulingFeature();
        sc.AddUtilities();

        return sc.BuildServiceProvider();
    }

    private static void AddCore(this IServiceCollection sc, string baseAddress)
    {
        sc.AddSingleton<PanelProxy>();
        sc.AddSingleton<IOverlayCloser, OverlayCloser>();
        sc.AddSingleton<ModalProxy>();
        sc.AddSingleton<IPanelFactory, PanelFactory>();
        sc.AddSingleton<IModalFactory, ModalFactory>();
        sc.AddSingleton<RemoteProxy>();
        sc.AddSingleton<SignalRNotificationClient>();
        sc.AddTransient<PanelHostView>();
        sc.AddTransient<ModalHostView>();

        sc.AddSingleton(new HttpClient { BaseAddress = new Uri(baseAddress) });
    }

    private static void AddShell(this IServiceCollection sc)
    {
        sc.AddSingleton<AppState>();
        sc.AddSingleton<ShellState>();
        sc.AddSingleton<UserState>();

        sc.AddSingleton<WorkspaceRouter>();

        // VMs
        sc.AddSingleton<AuthenticationWindowViewModel>();
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();
        sc.AddSingleton<WorkspaceNavigationViewModel>();
        sc.AddSingleton<UtilityNavigationViewModel>();
        sc.AddSingleton<FooterViewModel>();

        // Windows
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
    }

    private static void AddInfrastructure(this IServiceCollection sc)
    {
        sc.AddSingleton<IClipboardService, ClipboardService>();
        sc.AddSingleton<IAuthenticationService, AuthenticationService>();
        sc.AddSingleton<TimeZoneService>();
    }

    private static void AddInspector(this IServiceCollection sc)
    {
        sc.AddSingleton<ISelectionContextState, SelectionContextState>();
        
        sc.AddSingleton<InspectorGateway>();
        sc.AddSingleton<InspectorState>();
        sc.AddSingleton<InspectorFacadeViewModel>();
        sc.AddTransient<InspectorOverViewModel>();
    }

    private static void AddTasksFeature(this IServiceCollection sc)
    {
        sc.AddSingleton<TaskState>();
        sc.AddTransient<CreateTaskState>();
        sc.AddSingleton<TaskFacadeViewModel>();
        sc.AddSingleton<TaskGateway>();
        sc.AddSingleton<TaskToolbarView>();
        sc.AddSingleton<TaskContentView>();

        sc.AddTransient<CreateTaskView>();
        sc.AddTransient<CreateTaskViewModel>();
    }

    private static void AddSchedulingFeature(this IServiceCollection sc)
    {
        sc.AddSingleton<SchedulerState>();
        sc.AddTransient<CreateScheduleState>();
        sc.AddSingleton<SchedulerFacadeViewModel>();
        sc.AddSingleton<SchedulerGateway>();
        sc.AddSingleton<SchedulerToolbarView>();
        sc.AddSingleton<SchedulerContentView>();

        sc.AddTransient<CreateScheduleView>();
        sc.AddTransient<CreateScheduleViewModel>();
    }

    private static void AddUtilities(this IServiceCollection sc)
    {
        sc.AddSingleton<NotificationViewModel>();
    }
}
