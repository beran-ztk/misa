using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Misa.Ui.Avalonia.Features.Pages.Journal;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Features.Utilities.Dev;
using Misa.Ui.Avalonia.Features.Utilities.Notifications;
using Misa.Ui.Avalonia.Shell;
using Misa.Ui.Avalonia.Shell.Components;
using InspectorFacadeViewModel = Misa.Ui.Avalonia.Features.Inspector.InspectorFacadeViewModel;
using InspectorState = Misa.Ui.Avalonia.Features.Inspector.InspectorState;
using ShellWindowViewModel = Misa.Ui.Avalonia.Shell.ShellWindowViewModel;
using TaskFacadeViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.TaskFacadeViewModel;
using TaskState = Misa.Ui.Avalonia.Features.Pages.Tasks.TaskState;

namespace Misa.Ui.Avalonia.Infrastructure;

public static class CompositionRoot
{
    public static ServiceCollection Build(string baseAddress)
    {
        var sc = new ServiceCollection();
        
        sc.AddLogging(log => log.AddConsole());
        sc.AddSingleton(new HttpClient { BaseAddress = new Uri(baseAddress) });
        
        sc.AddCoreServices();
        sc.AddFeatureServices();
        sc.AddUtilityServices();

        return sc;
    }

    private static void AddCoreServices(this IServiceCollection sc)
    {
        sc.AddSingleton<ShellState>();
        sc.AddSingleton<TimeZoneService>();
        sc.AddSingleton<ISelectionContextState, SelectionContextState>();
        sc.AddSingleton<IWorkspaceHost>(sp => sp.GetRequiredService<ShellState>());
        
        // VMs
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();
        sc.AddSingleton<WorkspaceNavigationViewModel>();
        sc.AddSingleton<UtilityNavigationViewModel>();
        sc.AddSingleton<FooterViewModel>();

        sc.AddTransient<ShellWindow>(sp => new ShellWindow { DataContext = sp.GetRequiredService<ShellWindowViewModel>() });
    }

    private static void AddFeatureServices(this IServiceCollection sc)
    {
        // Inspector
        sc.AddSingleton<InspectorState>();
        sc.AddSingleton<InspectorFacadeViewModel>();
        
        // Task
        sc.AddSingleton<TaskState>();
        sc.AddSingleton<TaskFacadeViewModel>();
        
        // Journal
        sc.AddSingleton<JournalViewModel>();
        
        // Zettelkasten
        sc.AddSingleton<ZettelkastenViewModel>();
    }
    private static void AddUtilityServices(this IServiceCollection sc)
    {
        sc.AddSingleton<NotificationViewModel>();
        sc.AddSingleton<DevToolsViewModel>();
    }
}
