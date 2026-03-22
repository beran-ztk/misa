using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexNodeVm> KnowledgeIndex { get; } = [];
    private ZettelkastenGateway Gateway { get; }
    private readonly LayerProxy _layerProxy;

    [RelayCommand]
    private void BeginRenameSelectedItem()
    {
        SelectedNode?.BeginRenamingCommand.Execute(null);
    }

    [RelayCommand]
    private async Task DeleteSelectedItemAsync()
    {
        if (SelectedNode is null || SelectedNode.IsPendingCreation || SelectedNode.IsRenaming) return;
        await ConfirmAndDeleteAsync(SelectedNode);
    }

    [RelayCommand]
    private async Task DeleteItemAsync(Guid id)
    {
        var node = FindNode(id, KnowledgeIndex);
        if (node is null) return;
        await ConfirmAndDeleteAsync(node);
    }

    private async Task ConfirmAndDeleteAsync(KnowledgeIndexNodeVm rootNode)
    {
        var ids = CollectSubtreeIds(rootNode);

        var formVm = new DeleteKnowledgeItemViewModel(ids.ToArray(), Gateway);
        var result = await _layerProxy.OpenAsync<DeleteKnowledgeItemViewModel, Result>(formVm, LayerPresentation.Modal);

        if (result is not { IsSuccess: true }) return;

        // After delete: prefer parent, then an adjacent sibling.
        _reloadTargetId   = rootNode.ParentId;
        _reloadFallbackId = FindAdjacentSiblingId(rootNode);

        await LoadIndexAsync();
    }

    private static List<Guid> CollectSubtreeIds(KnowledgeIndexNodeVm node)
    {
        var ids = new List<Guid>();
        if (!node.IsPendingCreation)
            ids.Add(node.Id);
        foreach (var child in node.Children)
            ids.AddRange(CollectSubtreeIds(child));
        return ids;
    }

    [ObservableProperty] private KnowledgeIndexNodeVm? _selectedNode;
    [ObservableProperty] private string _breadcrumbPath = string.Empty;
    [ObservableProperty] private string _searchQuery    = string.Empty;

    internal bool SuppressExpansionPersistence { get; private set; }

    partial void OnSearchQueryChanged(string value)
    {
        var q = value.Trim();
        if (string.IsNullOrEmpty(q))
            ClearFilter();
        else
            ApplyFilter(q);
    }

    private void ApplyFilter(string query)
    {
        SuppressExpansionPersistence = true;
        foreach (var node in KnowledgeIndex)
            SetNodeVisibility(node, query);
        SuppressExpansionPersistence = false;
    }

    // Returns true when this node or any descendant is visible.
    private static bool SetNodeVisibility(KnowledgeIndexNodeVm node, string query)
    {
        bool titleMatch  = node.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
        bool childMatch  = false;
        foreach (var child in node.Children)
            childMatch |= SetNodeVisibility(child, query);

        node.IsSearchMatch = titleMatch;
        node.IsVisible     = titleMatch || childMatch;

        if (childMatch && !node.IsExpanded)
            node.IsExpanded = true;

        return node.IsVisible;
    }

    private void ClearFilter()
    {
        foreach (var node in KnowledgeIndex)
            ClearNodeFilter(node);
    }

    private static void ClearNodeFilter(KnowledgeIndexNodeVm node)
    {
        node.IsSearchMatch = false;
        node.IsVisible     = true;
        foreach (var child in node.Children)
            ClearNodeFilter(child);
    }

    [ObservableProperty] private ViewModelBase _zettelVm;

    [ObservableProperty] private bool _hasZettelSelected;

    private readonly ZettelkastenSettings _settings = ZettelkastenSettings.Load();
    private bool  _selectionRestored;
    private Guid? _reloadTargetId;
    private Guid? _reloadFallbackId;

    public ZettelkastenViewModel(ZettelkastenGateway gateway, LayerProxy layerProxy)
    {
        Gateway = gateway;
        _layerProxy = layerProxy;
        ZettelVm = new ZettelViewModel(Gateway);
    }

    partial void OnSelectedNodeChanged(KnowledgeIndexNodeVm? value)
    {
        BreadcrumbPath = value is not null ? BuildBreadcrumb(value) : string.Empty;

        if (value is not null && !value.IsPendingCreation)
        {
            _settings.LastSelectedItemId = value.Id;
            _settings.Save();
        }

        if (value is null || value.IsPendingCreation || value.Workflow != WorkflowDto.Zettel)
            return;

        _ = LoadZettelAsync(value.Id);
    }

    private string BuildBreadcrumb(KnowledgeIndexNodeVm node)
    {
        var parts = new System.Collections.Generic.List<string> { node.Title };
        var parentId = node.ParentId;
        while (parentId.HasValue)
        {
            var parent = FindNode(parentId.Value, KnowledgeIndex);
            if (parent is null) break;
            parts.Insert(0, parent.Title);
            parentId = parent.ParentId;
        }
        return string.Join(" / ", parts);
    }

    private void EnsureNodeVisible(KnowledgeIndexNodeVm node)
    {
        var parentId = node.ParentId;
        while (parentId.HasValue)
        {
            var parent = FindNode(parentId.Value, KnowledgeIndex);
            if (parent is null) break;
            if (!parent.IsExpanded)
            {
                parent.IsExpanded = true;
                _ = SetExpandedStateAsync(parent.Id, true);
            }
            parentId = parent.ParentId;
        }
    }

    private async Task LoadZettelAsync(Guid id)
    {
        var dto = await Gateway.GetZettelAsync(id);
        if (dto is null || ZettelVm is not ZettelViewModel vm) return;
        HasZettelSelected = true;
        vm.Load(dto);
    }

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

    [RelayCommand]
    private async Task OpenTrashAsync()
    {
        var vm = new TrashViewModel(Gateway);
        await _layerProxy.OpenAsync<TrashViewModel, Result>(vm, LayerPresentation.Panel);
        await LoadIndexAsync(); // refresh in case entries were restored
    }

    public async Task SetExpandedStateAsync(Guid id, bool expanded)
    {
        await Gateway.SetKnowledgeIndexExpandedStateAsync(id, expanded);
    }

    // ── Tree loading ──────────────────────────────────────────────────────────

    private async Task LoadIndexAsync()
    {
        var index = await Gateway.GetKnowledgeIndexAsync();
        if (index is null) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            KnowledgeIndex.Clear();
            foreach (var dto in index)
                KnowledgeIndex.Add(ToNodeVm(dto));

            var q = SearchQuery.Trim();
            if (!string.IsNullOrEmpty(q)) ApplyFilter(q);

            // First load: restore last-selected from settings.
            if (!_selectionRestored)
            {
                _selectionRestored = true;
                if (_settings.LastSelectedItemId.HasValue)
                {
                    var node = FindNode(_settings.LastSelectedItemId.Value, KnowledgeIndex);
                    if (node is not null) { EnsureNodeVisible(node); SelectedNode = node; }
                }
                return;
            }

            // Subsequent reloads: restore explicitly requested target.
            var targetId   = _reloadTargetId;
            var fallbackId = _reloadFallbackId;
            _reloadTargetId   = null;
            _reloadFallbackId = null;

            if (targetId.HasValue)
            {
                var node = FindNode(targetId.Value, KnowledgeIndex)
                        ?? (fallbackId.HasValue ? FindNode(fallbackId.Value, KnowledgeIndex) : null);
                if (node is not null) { EnsureNodeVisible(node); SelectedNode = node; }
                else                 { SelectedNode = null; HasZettelSelected = false; }
            }
        });
    }

    private KnowledgeIndexNodeVm ToNodeVm(KnowledgeIndexEntryDto dto)
    {
        var vm = new KnowledgeIndexNodeVm(async (id, title) =>
        {
            _reloadTargetId = id;
            var result = await Gateway.RenameItemAsync(id, new RenameItemRequest(title));
            if (result.IsSuccess)
                await LoadIndexAsync();
            else
                _reloadTargetId = null;
        })
        {
            Id         = dto.Id,
            Workflow   = dto.Workflow,
            Title      = dto.Title,
            ParentId   = dto.ParentId,
            IsExpanded = dto.IsExpanded
        };
        foreach (var child in dto.Children)
            vm.Children.Add(ToNodeVm(child));
        return vm;
    }

    // ── Drag-and-drop ─────────────────────────────────────────────────────────

    public async Task MoveItemAsync(Guid sourceId, Guid targetParentId)
    {
        if (sourceId == targetParentId) return;

        var source = FindNode(sourceId, KnowledgeIndex);
        var target = FindNode(targetParentId, KnowledgeIndex);

        if (source is null || target is null) return;
        if (target.Workflow != WorkflowDto.Topic) return;
        if (source.ParentId == targetParentId) return;        // already a child of this topic
        if (IsInSubtree(targetParentId, source)) return;      // target is inside source's subtree

        _reloadTargetId = sourceId;
        var result = await Gateway.ReparentItemAsync(sourceId, targetParentId);
        if (result.IsSuccess)
            await LoadIndexAsync();
        else
            _reloadTargetId = null;
    }

    // Returns true when candidateId lives anywhere in the subtree rooted at node.
    private static bool IsInSubtree(Guid candidateId, KnowledgeIndexNodeVm node)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsPendingCreation && child.Id == candidateId) return true;
            if (IsInSubtree(candidateId, child)) return true;
        }
        return false;
    }

    // ── Index item actions ────────────────────────────────────────────────────

    [RelayCommand]
    private void CreateTopicUnder(Guid parentId) =>
        BeginInlineCreation(parentId, WorkflowDto.Topic);

    [RelayCommand]
    private void CreateZettelUnder(Guid parentId) =>
        BeginInlineCreation(parentId, WorkflowDto.Zettel);

    private void BeginInlineCreation(Guid parentId, WorkflowDto workflow)
    {
        var parent = FindNode(parentId, KnowledgeIndex);
        if (parent is null || parent.Workflow != WorkflowDto.Topic) return;

        // Remove any existing pending row under this parent before adding a new one
        var existing = parent.Children.FirstOrDefault(c => c.IsPendingCreation);
        if (existing is not null)
            parent.Children.Remove(existing);

        var pending = new KnowledgeIndexNodeVm(null)
        {
            IsPendingCreation = true,
            PendingWorkflow   = workflow
        };

        pending.SetCallbacks(
            onCommit: async title =>
            {
                var result = workflow == WorkflowDto.Topic
                    ? await Gateway.CreateTopicAsync(new CreateTopicRequest(title, parentId))
                    : await Gateway.CreateZettelAsync(new CreateZettelRequest(title, parentId));

                if (result.IsSuccess)
                    await LoadIndexAsync();
                else
                    await Dispatcher.UIThread.InvokeAsync(() => parent.Children.Remove(pending));
            },
            onCancel: () =>
            {
                // ObservableCollection must be modified on the UI thread
                Dispatcher.UIThread.Post(() => parent.Children.Remove(pending));
            }
        );

        parent.Children.Insert(0, pending);
        parent.IsExpanded = true;
        _ = SetExpandedStateAsync(parent.Id, true);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns the ID of the sibling adjacent to node, preferring the one before it.
    private Guid? FindAdjacentSiblingId(KnowledgeIndexNodeVm node)
    {
        IEnumerable<KnowledgeIndexNodeVm> siblings = node.ParentId.HasValue
            ? FindNode(node.ParentId.Value, KnowledgeIndex)?.Children ?? []
            : KnowledgeIndex;

        var list = siblings.Where(c => !c.IsPendingCreation).ToList();
        var idx  = list.IndexOf(node);
        if (idx < 0) return null;
        if (idx > 0)              return list[idx - 1].Id;
        if (idx < list.Count - 1) return list[idx + 1].Id;
        return null;
    }

    private static KnowledgeIndexNodeVm? FindNode(
        Guid id, IEnumerable<KnowledgeIndexNodeVm> nodes)
    {
        foreach (var node in nodes)
        {
            if (!node.IsPendingCreation && node.Id == id) return node;
            var found = FindNode(id, node.Children);
            if (found is not null) return found;
        }
        return null;
    }
}
