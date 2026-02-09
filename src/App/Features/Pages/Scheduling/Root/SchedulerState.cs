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
    UserState userState,
    CreateScheduleState createScheduleState)
    : ObservableObject
{
    public CreateScheduleState CreateScheduleState { get; } = createScheduleState;
    public ObservableCollection<ScheduleDto> Schedules { get; } = [];

    [ObservableProperty] private ScheduleDto? _selectedItem;
    partial void OnSelectedItemChanged(ScheduleDto? value)
    {
        selectionContextState.SetActive(value?.Id);
    }
    public async Task AddToCollection(List<ScheduleDto> schedules)
    {
        foreach (var schedule in schedules)
        {
            await AddToCollection(schedule);
        }
    }
    public async Task AddToCollection(ScheduleDto? schedule)
    {
        if (schedule is null) return;
        
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Schedules.Add(schedule);
        });
    }
}