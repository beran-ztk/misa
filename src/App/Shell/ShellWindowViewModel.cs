using Misa.App.Infrastructure;
using Misa.App.Shell.Components;
using Misa.Application;

namespace Misa.App.Shell;

public partial class ShellWindowViewModel(
    Dispatcher dispatcher,
    HeaderViewModel header,
    NavigationViewModel navigation) : ViewModelBase(dispatcher)
{
    public ViewModelBase Header { get; } = header;
    public ViewModelBase Navigation { get; } = navigation;
    public ViewModelBase? Workspace { get; set; }
}
