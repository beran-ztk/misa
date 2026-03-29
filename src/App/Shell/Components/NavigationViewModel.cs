using System;
using System.Collections.Generic;
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
        var items = await Dispatcher.GetAsync(new GetItemsRequest());
        BuildTree(items);
    }

    private void BuildTree(List<Item> items)
    {
        var map = new Dictionary<Guid, IndexEntry>(items.Count);
        foreach (var item in items)
            map[item.Id] = new IndexEntry { Id = item.Id, ParentId = item.ParentId, Kind = item.Kind, Title = item.Title };

        IndexEntries.Clear();
        foreach (var entry in map.Values)
        {
            if (entry.ParentId is null || !map.TryGetValue(entry.ParentId.Value, out var parent))
                IndexEntries.Add(entry);
            else
                parent.Children.Add(entry);
        }
    }

    [RelayCommand]
    private async Task CreateRootTopic()
    {
        if (string.IsNullOrWhiteSpace(NewTopicTitle)) return;

        await Dispatcher.SendAsync(new CreateTopicRequest(null, NewTopicTitle));
        NewTopicTitle = string.Empty;
        await LoadAsync();
    }
}
