using System;
using System.Reactive;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.Features.Tasks.TaskNavigation;

public class TaskNavigationViewModel : ViewModelBase
{
    public Features.Tasks.TasksHub.TaskViewModel MainViewModel { get; }
    public ReactiveCommand<Unit, Unit> AddTaskCommand { get; }
    public TaskNavigationViewModel(Features.Tasks.TasksHub.TaskViewModel vm)
    {
        MainViewModel = vm;
        AddTaskCommand = ReactiveCommand.Create(AddTaskCommandAsync);
        AddTaskCommand
            .ThrownExceptions
            .Subscribe(Console.WriteLine);
    }
    private void AddTaskCommandAsync()
    {
        MainViewModel.IsCreateTaskFormOpen = true;
        MainViewModel.CurrentInfoModel = new Features.Tasks.AddTasks.AddTaskViewModel(MainViewModel);
    }
}