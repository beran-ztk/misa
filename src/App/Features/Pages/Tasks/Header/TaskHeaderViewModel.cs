using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Behaviors;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Add;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Root;
using AddTaskViewModel = Misa.Ui.Avalonia.Features.Pages.Tasks.Add.AddTaskViewModel;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Header;

public partial class TaskHeaderViewModel : ViewModelBase
{
    public TaskState TaskState { get; }
    public TaskHeaderViewModel(TaskState taskState)
    {
        TaskState = taskState;
        
        foreach (var p in Enum.GetValues<PriorityContract>())
        {
            var opt = new PriorityFilterOption(p, isSelected: true);
            opt.PropertyChanged += (s, e) =>
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
    private async Task ShowAddTaskWindow()
    {
        var vm = new AddTaskViewModel();
        var window = new AddTaskView
        {
            DataContext = vm
        };

        AddTaskDto? result = null;

        vm.Completed += dto =>
        {
            result = dto;
            window.Close();
        };

        vm.Cancelled += () => window.Close();

        var owner = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        if (owner is null)
        {
            window.Show();
            return;
        }

        await window.ShowDialog(owner);

        if (result != null)
            await TaskState.CreateTask(result);
    }
}