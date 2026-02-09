using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel(
    TaskState taskState, 
    TaskGateway taskGateway,
    PanelProxy panelProxy) 
    : ViewModelBase
{
    public TaskState TaskState { get; } = taskState;
    private TaskGateway TaskGateway { get; } = taskGateway;
    
    public async Task InitializeWorkspace()
    {
        await GetAllTasksAsync();
    }

    [RelayCommand]
    private void RefreshTaskWindow()
    {
        TaskState.SelectedTask = null;
        _ = GetAllTasksAsync();
    }

    private async Task GetAllTasksAsync()
    {
        var tasks = await TaskGateway.GetAllTasksAsync();
        
        await TaskState.AddToCollection(tasks);
    }
    [RelayCommand]
    private void ShowAddTaskPanel() => panelProxy.OpenAddTask(this);
    [RelayCommand]
    private void Close() => panelProxy.Close();
    [RelayCommand]
    private async Task SubmitCreateTask()
    {
        var dto = TaskState.CreateTaskState.TryGetValidatedRequestObject();
        if (dto is null) return;

        var task = await TaskGateway.CreateTaskAsync(dto);
        await TaskState.AddToCollection(task);
        Close();
    }
}