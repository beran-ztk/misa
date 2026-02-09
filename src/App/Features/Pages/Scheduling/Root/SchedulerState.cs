using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerState(
    ISelectionContextState selectionContextState,
    CreateScheduleState createState)
    : ObservableObject
{
    public CreateScheduleState CreateState { get; } = createState;
    public ObservableCollection<ScheduleDto> Items { get; } = [];
    public ObservableCollection<ScheduleDto> FilteredItems { get; } = [];

    [ObservableProperty] private ScheduleDto? _selectedItem;
    partial void OnSelectedItemChanged(ScheduleDto? value)
    {
        selectionContextState.SetActive(value?.Id);
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
    public async Task AddToCollection(List<ScheduleDto> items)
    {
        Items.Clear();
        FilteredItems.Clear();
        
        foreach (var item in items)
        {
            await AddToCollection(item);
        }
    }
    public async Task AddToCollection(ScheduleDto? item)
    {
        if (item is null) return;
        
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Items.Add(item);
            FilteredItems.Add(item);
        });
    }
}