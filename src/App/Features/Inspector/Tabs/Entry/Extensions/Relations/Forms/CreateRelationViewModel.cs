using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Common.Results;
using Misa.Contract.Items;
using Misa.Contract.Items.Components.Relations;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Relations.Forms;

public sealed partial class CreateRelationViewModel : ViewModelBase, IHostedForm<Result>
{
    private readonly Guid _currentItemId;
    private readonly List<ItemLookupDto> _allItems;
    private readonly InspectorGateway _gateway;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    private WorkflowFilterOption? _selectedWorkflowFilter;

    [ObservableProperty] private ItemLookupDto? _selectedItem;

    [ObservableProperty] private RelationTypeDto _selectedRelationType = RelationTypeDto.RelatedTo;

    public IReadOnlyList<WorkflowFilterOption> WorkflowOptions { get; }
    public IReadOnlyList<RelationTypeDto> RelationTypes { get; } = Enum.GetValues<RelationTypeDto>();

    public List<ItemLookupDto> FilteredItems
    {
        get
        {
            var items = _allItems.AsEnumerable();

            if (SelectedWorkflowFilter?.Value is { } workflow)
                items = items.Where(i => i.Workflow == workflow);

            if (!string.IsNullOrWhiteSpace(SearchText))
                items = items.Where(i => i.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            return items.ToList();
        }
    }

    public string FormTitle => "Add Relation";
    public string? FormDescription => null;

    public CreateRelationViewModel(Guid currentItemId, List<ItemLookupDto> allItems, InspectorGateway gateway)
    {
        _currentItemId = currentItemId;
        _allItems = allItems;
        _gateway = gateway;

        WorkflowOptions =
        [
            new WorkflowFilterOption(null, "All types"),
            .. Enum.GetValues<WorkflowDto>().Select(w => new WorkflowFilterOption(w, w.ToString()))
        ];

        _selectedWorkflowFilter = WorkflowOptions[0];
    }

    public async Task<Result<Result>> SubmitAsync()
    {
        if (SelectedItem is null)
            return Result<Result>.Failure();

        var request = new CreateRelationRequest(_currentItemId, SelectedItem.Id, SelectedRelationType);
        var result = await _gateway.CreateRelationAsync(request);

        return result.IsSuccess
            ? Result<Result>.Ok(Result.Ok())
            : Result<Result>.Failure();
    }
}

public sealed record WorkflowFilterOption(WorkflowDto? Value, string Display);
