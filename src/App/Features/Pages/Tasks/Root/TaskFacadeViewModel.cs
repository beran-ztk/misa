using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel(TaskState taskState, TaskGateway taskGateway) : ViewModelBase
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

    [RelayCommand]
    private void ShowAddTask()
    {
        var vm = new AddTaskViewModel();

        vm.Completed += async dto =>
        {
            await CreateTaskAsync(dto);

                TaskState.ShellState.Panel = null;
        };

        vm.Cancelled += () =>
        {
            TaskState.ShellState.Panel = null;
        };

        TaskState.ShellState.Panel = vm; 
    }
    
    private async Task GetAllTasksAsync()
    {
        var tasks = await TaskGateway.GetAllTasksAsync();
        
        await TaskState.AddToCollection(tasks ?? []);
    }
    private async Task CreateTaskAsync(AddTaskDto dto)
    {
        var task = await TaskGateway.CreateTaskAsync(dto);
        
        await TaskState.AddToCollection(task);
    }
}