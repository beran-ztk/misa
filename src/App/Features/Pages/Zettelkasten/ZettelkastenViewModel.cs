using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexNodeVm> Index { get; } = [];
    public List<ZettelDto> Zettels { get; private set; } = [];

    [ObservableProperty] private ZettelDto? _selectedZettel;
    [ObservableProperty] private string? _editableContent;
    [ObservableProperty] private string _saveStateLabel = string.Empty;

    private bool _suppressContentSave;
    private CancellationTokenSource? _saveCts;

    private KnowledgeIndexNodeVm? _pendingNode;
    private ObservableCollection<KnowledgeIndexNodeVm>? _pendingParentCollection;
    private Guid? _pendingParentId;

    /// <summary>
    /// Fired (on the UI thread) after a successful inline creation and tree rebuild.
    /// The argument is the new node so the view can select it.
    /// </summary>
    public event Action<KnowledgeIndexNodeVm>? ItemCreatedAndReady;

    // ── Initialization ────────────────────────────────────────────────────────

    public async Task InitializeWorkspaceAsync()
    {
        await LoadIndexAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await LoadIndexAsync();
    }

    // ── Zettel content editing ────────────────────────────────────────────────

    partial void OnSelectedZettelChanged(ZettelDto? value)
    {
        _suppressContentSave = true;
        EditableContent = value?.Content;
        SaveStateLabel = string.Empty;
        _suppressContentSave = false;
    }

    partial void OnEditableContentChanged(string? value)
    {
        if (_suppressContentSave) return;
        if (SelectedZettel is null) return;

        SaveStateLabel = "Unsaved";
        _ = AutoSaveAsync(SelectedZettel.Id, value);
    }

    private async Task AutoSaveAsync(Guid zettelId, string? content)
    {
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(1000, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (SelectedZettel?.Id != zettelId) return;

        var result = await gateway.UpdateZettelContentAsync(zettelId, content);

        if (result.IsSuccess)
        {
            var idx = Zettels.FindIndex(z => z.Id == zettelId);
            if (idx >= 0)
                Zettels[idx] = Zettels[idx] with { Content = content };

            if (SelectedZettel?.Id == zettelId)
                SaveStateLabel = "Saved";
        }
        else
        {
            if (SelectedZettel?.Id == zettelId)
                SaveStateLabel = "Save failed";
        }
    }

    // ── Inline creation ───────────────────────────────────────────────────────

    public void BeginInlineCreate(KnowledgeIndexNodeVm? context, WorkflowDto workflow)
    {
        CancelInlineCreate();

        ObservableCollection<KnowledgeIndexNodeVm> targetCollection;
        Guid? parentId;

        if (context is { IsPending: false })
        {
            if (workflow == WorkflowDto.Zettel && context.Entry?.Workflow != WorkflowDto.Topic)
                return;

            targetCollection = context.Children;
            parentId = context.Entry!.Id;
            context.IsExpanded = true;
        }
        else
        {
            if (workflow == WorkflowDto.Zettel) return;
            targetCollection = Index;
            parentId = null;
        }

        _pendingNode = new KnowledgeIndexNodeVm(workflow);
        _pendingParentCollection = targetCollection;
        _pendingParentId = parentId;
        targetCollection.Add(_pendingNode);
    }

    public void CancelInlineCreate()
    {
        if (_pendingNode is null) return;
        _pendingParentCollection?.Remove(_pendingNode);
        _pendingNode = null;
        _pendingParentCollection = null;
        _pendingParentId = null;
    }

    public async Task ConfirmInlineCreateAsync()
    {
        if (_pendingNode is null) return;

        var title = _pendingNode.PendingTitle.Trim();
        if (string.IsNullOrEmpty(title))
        {
            CancelInlineCreate();
            return;
        }

        var workflow = _pendingNode.PendingWorkflow;
        var parentId = _pendingParentId;

        // Snapshot before touching the tree so we can restore after reload.
        var expandedIds = SnapshotExpandedIds();
        if (parentId.HasValue)
            expandedIds.Add(parentId.Value); // ensure parent stays open
        var previousIds = SnapshotAllIds();  // to identify the new node after reload

        CancelInlineCreate();

        if (workflow == WorkflowDto.Topic)
        {
            var result = await gateway.CreateTopicAsync(new CreateTopicRequest(title, parentId));
            if (result.IsSuccess)
                await ReloadAndRestoreAsync(expandedIds, previousIds);
        }
        else if (workflow == WorkflowDto.Zettel)
        {
            if (parentId is null) return;
            var result = await gateway.CreateZettelAsync(new CreateZettelRequest(title, parentId.Value));
            if (result.IsSuccess)
                await ReloadAndRestoreAsync(expandedIds, previousIds);
        }
    }

    // ── Tree reload helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Full reload that resets expansion and selection. Used by init and manual refresh.
    /// </summary>
    private async Task LoadIndexAsync()
    {
        var indexResult = await gateway.GetZettelkastenAsync();

        if (indexResult is not null)
        {
            var tree = BuildIndexTree(indexResult);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Index.Clear();
                foreach (var entry in tree)
                    Index.Add(entry);

                SelectedZettel = null;
            });
        }
    }

    /// <summary>
    /// Reload that restores expanded state and fires ItemCreatedAndReady for the new node.
    /// Everything from tree rebuild onwards runs in one UI dispatch to avoid intermediate flicker.
    /// </summary>
    private async Task ReloadAndRestoreAsync(HashSet<Guid> expandedIds, HashSet<Guid> previousIds)
    {
        var indexResult = await gateway.GetZettelkastenAsync();


        if (indexResult is null) return;

        var tree = BuildIndexTree(indexResult);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Index.Clear();
            foreach (var entry in tree)
                Index.Add(entry);

            RestoreExpansion(Index, expandedIds);

            var newNode = FindNewNode(Index, previousIds);
            if (newNode is not null)
                ItemCreatedAndReady?.Invoke(newNode);
        });
    }

    // ── Expansion state ───────────────────────────────────────────────────────

    private HashSet<Guid> SnapshotExpandedIds()
    {
        var ids = new HashSet<Guid>();
        CollectExpandedIds(Index, ids);
        return ids;
    }

    private static void CollectExpandedIds(IEnumerable<KnowledgeIndexNodeVm> nodes, HashSet<Guid> ids)
    {
        foreach (var node in nodes)
        {
            if (node.IsPending || node.Entry is null || !node.IsExpanded) continue;
            ids.Add(node.Entry.Id);
            CollectExpandedIds(node.Children, ids);
        }
    }

    private HashSet<Guid> SnapshotAllIds()
    {
        var ids = new HashSet<Guid>();
        CollectAllIds(Index, ids);
        return ids;
    }

    private static void CollectAllIds(IEnumerable<KnowledgeIndexNodeVm> nodes, HashSet<Guid> ids)
    {
        foreach (var node in nodes)
        {
            if (node.IsPending || node.Entry is null) continue;
            ids.Add(node.Entry.Id);
            CollectAllIds(node.Children, ids);
        }
    }

    private static void RestoreExpansion(IEnumerable<KnowledgeIndexNodeVm> nodes, HashSet<Guid> expandedIds)
    {
        foreach (var node in nodes)
        {
            if (node.Entry is null) continue;
            if (!expandedIds.Contains(node.Entry.Id)) continue;
            node.IsExpanded = true;
            RestoreExpansion(node.Children, expandedIds);
        }
    }

    private static KnowledgeIndexNodeVm? FindNewNode(IEnumerable<KnowledgeIndexNodeVm> nodes, HashSet<Guid> previousIds)
    {
        foreach (var node in nodes)
        {
            if (node.Entry is not null && !previousIds.Contains(node.Entry.Id))
                return node;
            var found = FindNewNode(node.Children, previousIds);
            if (found is not null) return found;
        }
        return null;
    }

    // ── Tree building ─────────────────────────────────────────────────────────

    private static List<KnowledgeIndexNodeVm> BuildIndexTree(List<KnowledgeIndexEntryDto> entries)
    {
        var nodes = entries.ToDictionary(e => e.Id, e => new KnowledgeIndexNodeVm(e));

        var roots = entries
            .Where(e => e.ParentId is null)
            .OrderBy(e => e.Workflow)
            .ThenBy(e => e.Title)
            .Select(e => nodes[e.Id])
            .ToList();

        foreach (var entry in entries
                     .Where(e => e.ParentId is not null)
                     .OrderBy(e => e.Workflow)
                     .ThenBy(e => e.Title))
        {
            if (nodes.TryGetValue(entry.ParentId!.Value, out var parent))
                parent.Children.Add(nodes[entry.Id]);
        }

        return roots;
    }
}
