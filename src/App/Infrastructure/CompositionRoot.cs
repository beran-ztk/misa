using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Misa.Ui.Avalonia.Features.Pages.Zettelkasten;
using Misa.Ui.Avalonia.Shell;
using Misa.Ui.Avalonia.Shell.Components;

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

        return sc;
    }

    private static void AddCoreServices(this IServiceCollection sc)
    {
        // VMs
        sc.AddSingleton<ShellWindowViewModel>();
        sc.AddSingleton<HeaderViewModel>();

        sc.AddTransient<ShellWindow>(sp => new ShellWindow { DataContext = sp.GetRequiredService<ShellWindowViewModel>() });
    }

    private static void AddFeatureServices(this IServiceCollection sc)
    {
        // Zettelkasten
        sc.AddSingleton<ZettelkastenViewModel>();
    }
}
