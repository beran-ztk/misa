using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Schedules;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules.Root;

public sealed partial class ScheduleState : ObservableObject
{
    public ISelectionContextState SelectionContextState { get; }
    private IReadOnlyCollection<ScheduleDto> Items { get; set; } = [];
    public ObservableCollection<ScheduleDto> FilteredItems { get; } = [];

    [ObservableProperty] private ScheduleDto? _selectedItem;
    partial void OnSelectedItemChanged(ScheduleDto? value)
    {
        SelectionContextState.Set(value?.Item.Id);
    }

    public ScheduleState(ISelectionContextState selectionContextState)
    {
        SelectionContextState = selectionContextState;

        // Keep workspace list in sync when the Inspector navigates to an item externally.
        SelectionContextState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ISelectionContextState.ActiveEntityId)) return;
            var id = SelectionContextState.ActiveEntityId;
            if (SelectedItem?.Item.Id == id) return;
            SelectedItem = id.HasValue
                ? FilteredItems.FirstOrDefault(s => s.Item.Id == id.Value)
                : null;
        };
    }
    
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    private void ApplyFilters()
    {
        FilteredItems.Clear();
        
        foreach (var t in Items)
        {
            if (t.Item.Title.Contains((string)SearchText, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(SearchText))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => 
                {
                    FilteredItems.Add(t);
                });
            }
        }
    }
    public async Task AddToCollection(IReadOnlyCollection<ScheduleDto> items)
    {
        Items = items;
        FilteredItems.Clear();
        
        foreach (var item in items)
        {
            await AddToCollection(item);
        }
    }
    public async Task AddToCollection(ScheduleDto item)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            FilteredItems.Add(item);
        });
    }
}