using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Chronicle;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public sealed class ChronicleGroup
{
    public DateTime Date       { get; init; }
    public bool     IsUpcoming { get; init; }
    public bool     IsToday    { get; init; }
    public List<ChronicleEntryDto> Entries { get; init; } = [];
}

public partial class ChronicleViewModel : ViewModelBase
{
    private readonly ChronicleGateway          _gateway;
    private readonly ISelectionContextState    _selectionContextState;
    private readonly LayerProxy                _layerProxy;

    private IReadOnlyCollection<ChronicleEntryDto> _allEntries = [];

    public ObservableCollection<ChronicleGroup>           Groups      { get; } = [];
    public ObservableCollection<ChronicleTypeFilterOption> TypeFilters { get; } = [];

    [ObservableProperty] private string          _searchText = string.Empty;
    [ObservableProperty] private DateTimeOffset? _filterFrom;
    [ObservableProperty] private DateTimeOffset? _filterTo;

    public ChronicleViewModel(
        ChronicleGateway       gateway,
        ISelectionContextState selectionContextState,
        LayerProxy             layerProxy)
    {
        _gateway               = gateway;
        _selectionContextState = selectionContextState;
        _layerProxy            = layerProxy;

        _filterFrom = DateTimeOffset.UtcNow.AddDays(-30);
        _filterTo   = DateTimeOffset.UtcNow.AddDays(7);

        var enabledByDefault = new HashSet<ChronicleEntryType>
        {
            ChronicleEntryType.Journal,
            ChronicleEntryType.Deadline
        };

        foreach (var type in Enum.GetValues<ChronicleEntryType>())
        {
            var opt = new ChronicleTypeFilterOption(type, enabledByDefault.Contains(type));
            opt.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChronicleTypeFilterOption.IsSelected))
                    _ = RebuildGroupsAsync();
            };
            TypeFilters.Add(opt);
        }
    }

    partial void OnSearchTextChanged(string value)       => _ = RebuildGroupsAsync();
    partial void OnFilterFromChanged(DateTimeOffset? value) => _ = LoadAsync();
    partial void OnFilterToChanged(DateTimeOffset? value)   => _ = LoadAsync();

    // ── Data loading ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        if (FilterFrom is null || FilterTo is null) return;
        _allEntries = await _gateway.GetChronicleAsync(FilterFrom.Value, FilterTo.Value) ?? [];
        await RebuildGroupsAsync();
    }

    private async Task RebuildGroupsAsync()
    {
        var activeTypes = TypeFilters
            .Where(f => f.IsSelected)
            .Select(f => f.Type)
            .ToHashSet();

        var search = SearchText?.Trim();
        var today  = DateTime.Today;

        var groups = _allEntries
            .Where(e => activeTypes.Contains(e.Type))
            .Where(e => string.IsNullOrEmpty(search)
                        || e.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => e.At.ToLocalTime().Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new ChronicleGroup
            {
                Date       = g.Key,
                IsUpcoming = g.Key > today,
                IsToday    = g.Key == today,
                Entries    = g.OrderByDescending(e => e.At).ToList()
            })
            .ToList();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Groups.Clear();
            foreach (var g in groups)
                Groups.Add(g);
        });
    }

    // ── Selection ────────────────────────────────────────────────────────────

    public void SelectionChanged(ChronicleEntryDto? entry)
    {
        if (entry?.TargetItemId is null) return;
        _selectionContextState.Set(entry.TargetItemId.Value);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public async Task InitializeWorkspaceAsync() => await LoadAsync();

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new Journal.CreateJournalViewModel(_gateway);
        var result = await _layerProxy.OpenAsync<Journal.CreateJournalViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
            await LoadAsync();
    }
}
