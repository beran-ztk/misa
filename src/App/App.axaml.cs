using System;
using System.IO;
using System.Linq;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Misa.App.Shell;
using Misa.App.Shell.Components;
using Misa.App.Shell.Workspace;
using Misa.Application;
using Misa.Infrastructure;

namespace Misa.App;

public class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Misa", "misa.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        // ── Persistence ───────────────────────────────────────────────────────
        services.AddDbContextFactory<Context>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        // ── Application ───────────────────────────────────────────────────────
        services.AddSingleton<Repository>();
        services.AddSingleton<CreateItemHandler>();
        services.AddSingleton<Dispatcher>();

        // ── Shell ─────────────────────────────────────────────────────────────
        services.AddSingleton<HeaderViewModel>();
        services.AddSingleton<NavigationViewModel>();
        services.AddSingleton<NoteViewModel>();
        services.AddSingleton<ShellWindowViewModel>();
        services.AddTransient<ShellWindow>(sp => new ShellWindow
        {
            DataContext = sp.GetRequiredService<ShellWindowViewModel>()
        });

        var sp = services.BuildServiceProvider();

        // Ensure database schema exists
        using var db = sp.GetRequiredService<IDbContextFactory<Context>>().CreateDbContext();
        db.Database.EnsureCreated();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = sp.GetRequiredService<ShellWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var toRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in toRemove)
            BindingPlugins.DataValidators.Remove(plugin);
    }
}
