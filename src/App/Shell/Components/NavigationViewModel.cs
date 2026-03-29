using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.App.Infrastructure;
using Misa.Application;
using Misa.Domain;

namespace Misa.App.Shell.Components;

public partial class IndexEntry : ObservableObject
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public Kind Kind { get; init; }
    public string Title { get; init; } = string.Empty;
    public ObservableCollection<IndexEntry> Children { get; } = [];
}

public sealed partial class NavigationViewModel : ViewModelBase
{
    public ObservableCollection<IndexEntry> IndexEntries { get; } = [];

    [ObservableProperty] private string _newTopicTitle = string.Empty;

    public NavigationViewModel(Dispatcher dispatcher) : base(dispatcher)
    {
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var topics = await Dispatcher.GetAsync(new GetTopicsRequest());
    }
    
    [RelayCommand]
    private async Task CreateRootTopic()
    {
        if (string.IsNullOrWhiteSpace(NewTopicTitle)) return;

        await Dispatcher.SendAsync(new CreateTopicRequest(null, NewTopicTitle));
        NewTopicTitle = string.Empty;
    }
}
