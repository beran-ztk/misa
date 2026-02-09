using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Add;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Toolbar;

public sealed partial class SchedulerToolbarViewModel : ViewModelBase
{
    public SchedulerState SchedulerState { get; }
    private ShellState ShellState { get; }

    public SchedulerToolbarViewModel(SchedulerState schedulerState, ShellState shellState)
    {
        SchedulerState = schedulerState;
        ShellState = shellState;
    }

    [RelayCommand]
    private void ShowAddSchedule()
    {
        var vm = new AddScheduleViewModel();

        vm.Completed += async dto =>
        {
            await SchedulerState.CreateSchedule(dto);
            ShellState.Panel = null;
        };

        vm.Cancelled += () =>
        {
            ShellState.Panel = null;
        };

        // ShellState.Panel = vm;
    }
}