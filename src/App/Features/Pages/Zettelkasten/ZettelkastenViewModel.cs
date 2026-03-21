using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexNodeVm> KnowledgeIndex { get; } = [];

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

    public async Task SetExpandedStateAsync(Guid id, bool expanded)
    {
        await gateway.SetKnowledgeIndexExpandedStateAsync(id, expanded);
    }

    // ── Tree loading ──────────────────────────────────────────────────────────

    private async Task LoadIndexAsync()
    {
        var index = await gateway.GetKnowledgeIndexAsync();
        if (index is null) return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            KnowledgeIndex.Clear();
            foreach (var dto in index)
                KnowledgeIndex.Add(ToNodeVm(dto));
        });
    }

    private static KnowledgeIndexNodeVm ToNodeVm(KnowledgeIndexEntryDto dto)
    {
        var vm = new KnowledgeIndexNodeVm
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

        var pending = new KnowledgeIndexNodeVm
        {
            IsPendingCreation = true,
            PendingWorkflow   = workflow
        };

        pending.SetCallbacks(
            onCommit: async title =>
            {
                var result = workflow == WorkflowDto.Topic
                    ? await gateway.CreateTopicAsync(new CreateTopicRequest(title, parentId))
                    : await gateway.CreateZettelAsync(new CreateZettelRequest(title, parentId));

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
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
