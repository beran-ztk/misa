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
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanHaveChildren))] private Kind _kind;
    [ObservableProperty] private string _title = string.Empty;
    public ObservableCollection<IndexEntry> Children { get; } = [];
    public bool CanHaveChildren => Kind == Kind.Topic;
    public bool HasChildren => Children.Count > 0;

    public IndexEntry()
    {
        Children.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChildren));
    }

    // ── Expansion State ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isExpanded;
    public required Func<UpdateExpansionStateRequest, Task> OnExpansionStateChange { get; init; }
    partial void OnIsExpandedChanging(bool value)
    {
        _ = OnExpansionStateChange.Invoke(new UpdateExpansionStateRequest(Id, value));
    }

    // ── Selection ────────────────────────────────────────────────────────
    [ObservableProperty] private bool _isSelected;
    public required Action<IndexEntry> OnSelect { get; init; }
    [RelayCommand] private void Select() => OnSelect(this);

    // ── Rename ──────────────────────────────────────────────────
    [ObservableProperty] private bool _isRenaming;
    [ObservableProperty] private string _renamedTitle = string.Empty;
    public required Func<UpdateTitleRequest, Task<bool>> OnRename { get; init; }

    [RelayCommand] private void IntendToRename() { RenamedTitle = Title; IsRenaming = true; }
    [RelayCommand] private void CancelRename() => IsRenaming = false;

    [RelayCommand]
    private async Task SubmitRename()
    {
        var result = await OnRename.Invoke(new UpdateTitleRequest(Id, RenamedTitle));

        IsRenaming = false;

        if (result)
            Title = RenamedTitle;
    }

    // ── Child creation ──────────────────────────────────────────────────
    public required Func<Kind, Guid?, Task<IndexEntry?>> OnCreateIndex { get; init; }

    [RelayCommand] private void CreateTopic() => _ = CreateIndex(Kind.Topic);
    [RelayCommand] private void CreateNote()  => _ = CreateIndex(Kind.Note);
    [RelayCommand] private void CreateQuest() => _ = CreateIndex(Kind.Quest);

    [RelayCommand]
    private async Task CreateIndex(Kind kind)
    {
        IsExpanded = true;

        var entry = await OnCreateIndex.Invoke(kind, Id);
        if (entry is null) return;

        Children.Add(entry);
    }
}

public sealed partial class NavigationViewModel : ViewModelBase
{
    public ObservableCollection<IndexEntry> IndexEntries { get; } = [];
    
    public NavigationViewModel(Dispatcher dispatcher) : base(dispatcher)
    {
        _ = LoadAsync();
    }

    [RelayCommand] private async Task Reload() => await LoadAsync();
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
            var entry = CreateIndexEntry(item);
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

    [ObservableProperty] private IndexEntry? _selectedEntry;

    private void SelectEntry(IndexEntry entry)
    {
        if (SelectedEntry is not null)
            SelectedEntry.IsSelected = false;
        SelectedEntry = entry;
        entry.IsSelected = true;
    }

    private IndexEntry CreateIndexEntry(Item item) => new()
    {
        OnExpansionStateChange = ExpansionStateChange,
        OnCreateIndex = CreateIndexAsync,
        OnRename = Rename,
        OnSelect = SelectEntry,

        Id = item.Id,
        ParentId = item.ParentId,
        Kind = item.Kind,
        Title = item.Title,
        IsExpanded = item.IsExpanded
    };
    private async Task<bool> Rename(UpdateTitleRequest r) => await Dispatcher.UpdateAsync(r);
    private async Task ExpansionStateChange(UpdateExpansionStateRequest r) => await Dispatcher.UpdateAsync(r);

    [RelayCommand]
    private async Task CreateRootTopic()
    {
        var entry = await CreateIndexAsync(Kind.Topic, null);
        if (entry is null) return;
        
        IndexEntries.Add(entry);
    }
    private async Task<IndexEntry?> CreateIndexAsync(Kind kind, Guid? parentId)
    {
        const string title = "Untitled";

        var item = kind switch
        {
            Kind.Topic => await Dispatcher.SendAsync(new CreateTopicRequest(parentId, title)),
            Kind.Note => await Dispatcher.SendAsync(new CreateNoteRequest(parentId, title)),
            Kind.Quest => await Dispatcher.SendAsync(new CreateQuestRequest(parentId, title)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
        if (item is null) return null;
        
        var entry = CreateIndexEntry(item);
        entry.IntendToRenameCommand.Execute(null);
        
        return entry;
    }
}
