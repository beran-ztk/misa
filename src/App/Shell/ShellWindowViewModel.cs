using System;
using Microsoft.Extensions.DependencyInjection;
using Misa.App.Infrastructure;
using Misa.App.Shell.Components;

namespace Misa.App.Shell;

public partial class ShellWindowViewModel : ViewModelBase
{
    private IServiceProvider ServiceProvider { get; }
    public ViewModelBase Header { get; init; }
    public ViewModelBase Navigation { get; init; }
    public ViewModelBase? Workspace { get; set; }

    public ShellWindowViewModel(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        Header = ServiceProvider.GetRequiredService<HeaderViewModel>();
        Navigation = ServiceProvider.GetRequiredService<NavigationViewModel>();
    }
}