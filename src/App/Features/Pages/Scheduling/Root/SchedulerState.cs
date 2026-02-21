using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Schedules;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerState(
    ISelectionContextState selectionContextState,
    CreateScheduleState createState)
    : ObservableObject
{
    public CreateScheduleState CreateState { get; } = createState;
    private IReadOnlyCollection<ScheduleExtensionDto> Items { get; set; } = [];
    public ObservableCollection<ScheduleExtensionDto> FilteredItems { get; } = [];

    [ObservableProperty] private ScheduleExtensionDto? _selectedItem;
    partial void OnSelectedItemChanged(ScheduleExtensionDto? value)
    {
        selectionContextState.Set(value?.Id);
    }
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    private void ApplyFilters()
    {
        FilteredItems.Clear();
        
        foreach (var t in Items)
        {
            if (t.Item.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(SearchText))
            {
                _ = Dispatcher.UIThread.InvokeAsync(() => 
                {
                    FilteredItems.Add(t);
                });
            }
        }
    }
    public async Task AddToCollection(IReadOnlyCollection<ScheduleExtensionDto> items)
    {
        Items = items;
        FilteredItems.Clear();
        
        foreach (var item in items)
        {
            await AddToCollection(item);
        }
    }
    public async Task AddToCollection(ScheduleExtensionDto item)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            FilteredItems.Add(item);
        });
    }
}