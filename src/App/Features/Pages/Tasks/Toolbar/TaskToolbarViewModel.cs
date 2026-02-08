using System;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Toolbar;

public partial class TaskToolbarViewModel : ViewModelBase
{
    public TaskState TaskState { get; }
    private ShellState ShellState { get; }

    public TaskToolbarViewModel(TaskState taskState, ShellState shellState)
    {
        TaskState = taskState;
        ShellState = shellState;

        foreach (var p in Enum.GetValues<PriorityContract>())
        {
            var opt = new PriorityFilterOption(p, isSelected: true);
            opt.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PriorityFilterOption.IsSelected))
                    TaskState.ApplyFilters();
            };
            TaskState.PriorityFilters.Add(opt);
        }
    }

    [RelayCommand]
    private void RefreshTaskWindow()
    {
        TaskState.SelectedTask = null;
        _ = TaskState.LoadAsync();
    }

    [RelayCommand]
    private void ShowAddTask()
    {
        var vm = new AddTaskViewModel();

        vm.Completed += async dto =>
        {
            await TaskState.CreateTask(dto);

            ShellState.Panel = null;
        };

        vm.Cancelled += () =>
        {
            ShellState.Panel = null;
        };

        ShellState.Panel = vm; 
    }
}
