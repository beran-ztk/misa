using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Relations;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Relations;

public sealed partial class InspectorRelationsViewModel : ViewModelBase
{
    private readonly InspectorFacadeViewModel _facade;
    private readonly InspectorGateway _gateway;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRelations))]
    private List<RelationRowVm> _relationRows = [];

    public bool HasRelations => RelationRows.Count > 0;

    [ObservableProperty] private bool _isCreateFormOpen;

    [ObservableProperty] private List<ItemLookupDto> _lookupItems = [];
    [ObservableProperty] private ItemLookupDto? _selectedTargetItem;
    [ObservableProperty] private RelationTypeDto _selectedRelationType = RelationTypeDto.RelatedTo;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public List<ItemLookupDto> FilteredLookupItems =>
        string.IsNullOrWhiteSpace(SearchText)
            ? LookupItems
            : LookupItems
                .Where(i => !string.IsNullOrWhiteSpace(i.Title) &&
                            i.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

    public IReadOnlyList<RelationTypeDto> RelationTypes { get; } = Enum.GetValues<RelationTypeDto>();

    public InspectorRelationsViewModel(InspectorFacadeViewModel facade, InspectorGateway gateway)
    {
        _facade = facade;
        _gateway = gateway;

        facade.State.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(InspectorState.Item))
                _ = LoadRelationsAsync();
        };

        PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SearchText) || e.PropertyName == nameof(LookupItems))
            OnPropertyChanged(nameof(FilteredLookupItems));
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

    // ── Create form ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenCreateForm()
    {
        if (LookupItems.Count == 0)
        {
            var result = await _gateway.GetItemsForLookupAsync();
            if (result.IsSuccess && result.Value is not null)
            {
                LookupItems = result.Value
                    .Where(i => i.Id != _facade.State.Item.Id)
                    .ToList();
            }
        }

        SearchText = string.Empty;
        SelectedTargetItem = null;
        SelectedRelationType = RelationTypeDto.RelatedTo;
        IsCreateFormOpen = true;
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreateFormOpen = false;
        SearchText = string.Empty;
        SelectedTargetItem = null;
    }

    [RelayCommand]
    private async Task SubmitCreate()
    {
        if (SelectedTargetItem is null) return;

        var request = new CreateRelationRequest(
            _facade.State.Item.Id,
            SelectedTargetItem.Id,
            SelectedRelationType
        );

        var result = await _gateway.CreateRelationAsync(request);
        if (!result.IsSuccess) return;

        IsCreateFormOpen = false;
        SearchText = string.Empty;
        SelectedTargetItem = null;
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
