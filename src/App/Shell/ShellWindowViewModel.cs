using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.App.Infrastructure;
using Misa.App.Shell.Components;
using Misa.App.Shell.Workspace;
using Misa.Application;

namespace Misa.App.Shell;

public sealed partial class ShellWindowViewModel : ViewModelBase
{
    public ViewModelBase Header { get; }
    public ViewModelBase Navigation { get; }
    [ObservableProperty] private ViewModelBase? _workspace;

    private readonly NoteViewModel _noteViewModel;

    public ShellWindowViewModel(
        Dispatcher dispatcher,
        HeaderViewModel header,
        NavigationViewModel navigation,
        NoteViewModel noteViewModel)
        : base(dispatcher)
    {
        Header = header;
        Navigation = navigation;
        _noteViewModel = noteViewModel;
        navigation.OnNodeOpened = OpenNodeAsync;
    }

    private async Task OpenNodeAsync(Guid id)
    {
        var item = await Dispatcher.GetAsync(new GetItemRequest(id));
        if (item is null) return;
        _noteViewModel.Load(item);
        Workspace = _noteViewModel;
    }
}
