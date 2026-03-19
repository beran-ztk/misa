using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Features.Pages.Chronicle;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Create;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Root;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Features.Utilities.Dev;
using Misa.Ui.Avalonia.Features.Utilities.Notifications;
using Misa.Ui.Avalonia.Infrastructure.Client.RemoteProxy;
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
    public static ServiceCollection Build(string baseAddress)
    {
        var sc = new ServiceCollection();

        sc.AddCore(baseAddress);
        sc.AddShell();
        sc.AddInfrastructure();
        sc.AddInspector();
        sc.AddTasksFeature();
        sc.AddSchedulingFeature();
        sc.AddChronicleFeature();
        sc.ZettelkastenServices();
        sc.AddUtilities();

        sc.AddLogging(log => log.AddConsole());

        return sc;
    }

    private static void AddCore(this IServiceCollection sc, string baseAddress)
    {
        sc.AddSingleton<LayerProxy>();
        sc.AddSingleton<ILayerCloser>(sp => sp.GetRequiredService<LayerProxy>());
        sc.AddSingleton<RemoteProxy>();
        sc.AddSingleton<SignalRNotificationClient>(sp =>
            new SignalRNotificationClient(
                sp.GetRequiredService<UserState>(),
                baseAddress + "/hubs/updates"));
        sc.AddTransient<LayerHostView>();

        sc.AddSingleton(new HttpClient { BaseAddress = new Uri(baseAddress) });
    }

    private static void AddShell(this IServiceCollection sc)
    {
        sc.AddSingleton<UserState>();
        sc.AddSingleton<ShellState>();

        sc.AddSingleton<IWorkspaceHost>(sp => sp.GetRequiredService<ShellState>());
        sc.AddSingleton<ILayerHost>(sp => sp.GetRequiredService<ShellState>());
        sc.AddSingleton<IToastHost>(sp => sp.GetRequiredService<ShellState>());
        
        // VMs
        sc.AddTransient<AuthenticationWindowViewModel>();
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();
        sc.AddSingleton<WorkspaceNavigationViewModel>();
        sc.AddSingleton<UtilityNavigationViewModel>();
        sc.AddSingleton<FooterViewModel>();

        // Windows — must be Transient: Avalonia does not allow showing a closed window instance
        sc.AddTransient<AuthenticationWindow>(sp =>
        {
            var vm = sp.GetRequiredService<AuthenticationWindowViewModel>();
            return new AuthenticationWindow { DataContext = vm };
        });

        sc.AddTransient<ShellWindow>(sp =>
        {
            var vm = sp.GetRequiredService<ShellWindowViewModel>();
            return new ShellWindow { DataContext = vm };
        });
    }

    private static void AddInfrastructure(this IServiceCollection sc)
    {
        sc.AddSingleton<IClipboardService, ClipboardService>();
        sc.AddSingleton<AuthenticationGateway>();
        sc.AddSingleton<TimeZoneService>();
    }

    private static void AddInspector(this IServiceCollection sc)
    {
        sc.AddSingleton<ISelectionContextState, SelectionContextState>();
        
        sc.AddSingleton<InspectorGateway>();
        sc.AddSingleton<InspectorState>();
        sc.AddSingleton<InspectorFacadeViewModel>();
        
        sc.AddTransient<StartSessionView>();
        sc.AddTransient<PauseSessionView>();
        sc.AddTransient<EndSessionView>();
    }

    private static void AddTasksFeature(this IServiceCollection sc)
    {
        sc.AddSingleton<TaskState>();
        sc.AddSingleton<TaskFacadeViewModel>();
        sc.AddSingleton<TaskGateway>();

        sc.AddTransient<CreateTaskView>();
    }

    private static void AddSchedulingFeature(this IServiceCollection sc)
    {
        sc.AddSingleton<ScheduleState>();
        sc.AddSingleton<ScheduleFacadeViewModel>();
        sc.AddSingleton<ScheduleGateway>();

        sc.AddTransient<CreateScheduleView>();
        sc.AddTransient<CreateScheduleViewModel>();
    }
    private static void AddChronicleFeature(this IServiceCollection sc)
    {
        sc.AddSingleton<ChronicleViewModel>();
        sc.AddSingleton<ChronicleGateway>();
        
        sc.AddTransient<CreateJournalView>();
        sc.AddTransient<CreateJournalViewModel>();
    }
    
    private static void ZettelkastenServices(this IServiceCollection sc)
    {
        sc.AddSingleton<ZettelkastenViewModel>();
        sc.AddSingleton<ZettelkastenGateway>();
    }

    private static void AddUtilities(this IServiceCollection sc)
    {
        sc.AddSingleton<NotificationGateway>();
        sc.AddSingleton<NotificationViewModel>();

        sc.AddSingleton<DevGateway>();
        sc.AddSingleton<DevToolsViewModel>();
    }
}
