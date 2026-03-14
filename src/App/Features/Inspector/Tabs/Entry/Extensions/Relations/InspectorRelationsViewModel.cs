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
        {
            OnPropertyChanged(nameof(FilteredLookupItems));
        }
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
            .Select(r => new RelationRowVm(
                r.RelationType,
                r.SourceItemId == itemId ? "→" : "←",
                r.SourceItemId == itemId ? r.TargetItemTitle : r.SourceItemTitle
            ))
            .ToList();
    }

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
}

/// <summary>Flat display row for one relation.</summary>
public sealed record RelationRowVm(string RelationType, string Direction, string OtherTitle);