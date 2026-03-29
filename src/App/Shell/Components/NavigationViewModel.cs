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

    // ── Child creation state ──────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTopicSelected))]
    [NotifyPropertyChangedFor(nameof(IsNoteSelected))]
    [NotifyPropertyChangedFor(nameof(IsQuestSelected))]
    private Kind _pendingKind = Kind.Topic;

    [ObservableProperty] private string _pendingTitle = string.Empty;

    public bool IsTopicSelected => PendingKind == Kind.Topic;
    public bool IsNoteSelected  => PendingKind == Kind.Note;
    public bool IsQuestSelected => PendingKind == Kind.Quest;

    [RelayCommand] private void SetKindTopic() => PendingKind = Kind.Topic;
    [RelayCommand] private void SetKindNote()  => PendingKind = Kind.Note;
    [RelayCommand] private void SetKindQuest() => PendingKind = Kind.Quest;

    // Wired by NavigationViewModel during BuildTree
    internal Func<IndexEntry, Task>? OnCreateChild { get; set; }

    [RelayCommand]
    private Task CreateChild() => OnCreateChild?.Invoke(this) ?? Task.CompletedTask;
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
        {
            var entry = new IndexEntry
            {
                Id       = item.Id,
                ParentId = item.ParentId,
                Kind     = item.Kind,
                Title    = item.Title,
            };
            entry.OnCreateChild = CreateChildAsync;
            map[item.Id] = entry;
        }

        IndexEntries.Clear();
        foreach (var entry in map.Values)
        {
            if (entry.ParentId is null || !map.TryGetValue(entry.ParentId.Value, out var parent))
                IndexEntries.Add(entry);
            else
                parent.Children.Add(entry);
        }
    }

    private async Task CreateChildAsync(IndexEntry parent)
    {
        if (string.IsNullOrWhiteSpace(parent.PendingTitle)) return;

        switch (parent.PendingKind)
        {
            case Kind.Topic:
                await Dispatcher.SendAsync(new CreateTopicRequest(parent.Id, parent.PendingTitle));
                break;
            case Kind.Note:
                await Dispatcher.SendAsync(new CreateNoteRequest(parent.Id, parent.PendingTitle));
                break;
            case Kind.Quest:
                await Dispatcher.SendAsync(new CreateQuestRequest(parent.Id, parent.PendingTitle));
                break;
        }

        parent.PendingTitle = string.Empty;
        await LoadAsync();
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
