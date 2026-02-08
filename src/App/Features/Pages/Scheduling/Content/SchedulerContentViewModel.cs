using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Content;

public class SchedulerContentViewModel : ViewModelBase
{
    public SchedulerState SchedulerState { get; set; }
    public SchedulerContentViewModel(SchedulerState schedulerState)
    {
        SchedulerState = schedulerState;
        _ = SchedulerState.LoadSchedulesAsync();
    }
}