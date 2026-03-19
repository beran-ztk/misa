using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Relations;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Relations.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Relations;

public sealed partial class InspectorRelationsViewModel : ViewModelBase
{
    private readonly InspectorFacadeViewModel _facade;
    private readonly InspectorGateway _gateway;
    private readonly LayerProxy _layerProxy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRelations))]
    private List<RelationRowVm> _relationRows = [];

    public bool HasRelations => RelationRows.Count > 0;

    public IReadOnlyList<RelationTypeDto> RelationTypes { get; } = Enum.GetValues<RelationTypeDto>();

    public InspectorRelationsViewModel(InspectorFacadeViewModel facade, InspectorGateway gateway, LayerProxy layerProxy)
    {
        _facade = facade;
        _gateway = gateway;
        _layerProxy = layerProxy;

        facade.State.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(InspectorState.Item))
                _ = LoadRelationsAsync();
        };
    }

    private async Task LoadRelationsAsync()
    {
        var itemId = _facade.State.Item.Id;
        if (itemId == Guid.Empty)
        {
            RelationRows = [];
            return;
        }

        var result = await _gateway.GetRelationsAsync(itemId);
        if (!result.IsSuccess || result.Value is null)
        {
            RelationRows = [];
            return;
        }

        RelationRows = result.Value
            .Select(r =>
            {
                var isSource      = r.SourceItemId == itemId;
                var chipText      = ChipText(r.RelationType);
                var label         = RelationLabel(r.RelationType, isSource);
                var otherId       = isSource ? r.TargetItemId   : r.SourceItemId;
                var otherTitle    = isSource ? r.TargetItemTitle : r.SourceItemTitle;
                var otherWorkflow = isSource ? r.TargetItemWorkflow : r.SourceItemWorkflow;
                return new RelationRowVm(r.RelationId, r.RelationType, chipText, label, otherId, otherTitle, otherWorkflow.ToString());
            })
            .ToList();
    }

    // ── Create (modal) ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenCreateModal()
    {
        var lookupResult = await _gateway.GetItemsForLookupAsync();
        if (!lookupResult.IsSuccess || lookupResult.Value is null) return;

        var items = lookupResult.Value
            .Where(i => i.Id != _facade.State.Item.Id)
            .ToList();

        var formVm = new CreateRelationViewModel(_facade.State.Item.Id, items, _gateway);
        var result = await _layerProxy.OpenAsync<CreateRelationViewModel, Result>(formVm, LayerPresentation.Modal);

        if (result is { IsSuccess: true })
            await LoadRelationsAsync();
    }

    // ── Edit ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void StartEdit(RelationRowVm row)
    {
        foreach (var r in RelationRows)
            r.IsEditing = false;
        row.EditingType = row.RelationType;
        row.IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit(RelationRowVm row)
    {
        row.IsEditing = false;
    }

    [RelayCommand]
    private async Task SubmitEdit(RelationRowVm row)
    {
        var result = await _gateway.UpdateRelationAsync(row.RelationId, new UpdateRelationRequest(row.EditingType));
        if (!result.IsSuccess) return;
        row.IsEditing = false;
        await LoadRelationsAsync();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task DeleteRelation(RelationRowVm row)
    {
        var result = await _gateway.DeleteRelationAsync(row.RelationId);
        if (!result.IsSuccess) return;
        await LoadRelationsAsync();
    }

    // ── Navigate ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void NavigateToItem(Guid itemId)
    {
        _facade.ContextState.Set(itemId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string ChipText(RelationTypeDto type) => type switch
    {
        RelationTypeDto.RelatedTo   => "related to",
        RelationTypeDto.References  => "references",
        RelationTypeDto.DerivedFrom => "derived from",
        RelationTypeDto.DuplicateOf => "duplicate of",
        RelationTypeDto.Contains    => "contains",
        _                           => type.ToString()
    };

    private static string RelationLabel(RelationTypeDto type, bool isSource) => type switch
    {
        RelationTypeDto.RelatedTo   => "related to",
        RelationTypeDto.References  => isSource ? "references"        : "is referenced by",
        RelationTypeDto.DerivedFrom => isSource ? "is derived from"   : "is the basis for",
        RelationTypeDto.DuplicateOf => isSource ? "is a duplicate of" : "has duplicate",
        RelationTypeDto.Contains    => isSource ? "contains"          : "is contained in",
        _                           => type.ToString()
    };
}

/// <summary>Observable row VM for one relation — supports inline edit.</summary>
public sealed partial class RelationRowVm : ObservableObject
{
    public Guid RelationId { get; }
    public RelationTypeDto RelationType { get; }
    public string ChipLabel { get; }
    public string RelationLabel { get; }
    public Guid OtherItemId { get; }
    public string OtherTitle { get; }
    public string OtherWorkflow { get; }

    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private RelationTypeDto _editingType;

    public RelationRowVm(
        Guid relationId,
        RelationTypeDto relationType,
        string chipLabel,
        string relationLabel,
        Guid otherItemId,
        string otherTitle,
        string otherWorkflow)
    {
        RelationId    = relationId;
        RelationType  = relationType;
        ChipLabel     = chipLabel;
        RelationLabel = relationLabel;
        OtherItemId   = otherItemId;
        OtherTitle    = otherTitle;
        OtherWorkflow = otherWorkflow;
        _editingType  = relationType;
    }
}
