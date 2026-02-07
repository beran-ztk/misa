using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Content;

public class TaskContentViewModel : ViewModelBase
{
    public TaskState TaskState { get; init; } 
    public TaskContentViewModel(TaskState taskState)
    {
        TaskState = taskState;
        _ = TaskState.LoadAsync();
    }
}