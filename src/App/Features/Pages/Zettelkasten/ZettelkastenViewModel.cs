using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexNodeVm> KnowledgeIndex { get; } = [];
    private ZettelkastenGateway Gateway { get; }

    [RelayCommand]
    private void BeginRenameSelectedItem()
    {
        SelectedNode?.BeginRenamingCommand.Execute(null);
    }

    [ObservableProperty] private KnowledgeIndexNodeVm? _selectedNode;
    
    [ObservableProperty] private ViewModelBase _zettelVm;

    [ObservableProperty] private bool _hasZettelSelected;

    public ZettelkastenViewModel(ZettelkastenGateway gateway)
    {
        Gateway = gateway;
        ZettelVm = new ZettelViewModel(Gateway);
    }
    partial void OnSelectedNodeChanged(KnowledgeIndexNodeVm? value)
    {
        if (value is null || value.IsPendingCreation || value.Workflow != WorkflowDto.Zettel)
        {
            return;
        }
        
        _ = LoadZettelAsync(value.Id);
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
        });
    }

    private KnowledgeIndexNodeVm ToNodeVm(KnowledgeIndexEntryDto dto)
    {
        var vm = new KnowledgeIndexNodeVm(async (id, title) =>
        {
            var result = await Gateway.RenameItemAsync(id, new RenameItemRequest(title));
            if (result.IsSuccess)
                await LoadIndexAsync();
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
