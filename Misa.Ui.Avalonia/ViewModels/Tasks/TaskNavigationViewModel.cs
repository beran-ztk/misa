using System;
using System.Reactive;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Tasks;

public class TaskNavigationViewModel : ViewModelBase
{
    public TaskViewModel MainViewModel { get; }
    public ReactiveCommand<Unit, Unit> AddTaskCommand { get; }
    public TaskNavigationViewModel(TaskViewModel vm)
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
        MainViewModel.CurrentInfoModel = new TaskCreateViewModel(MainViewModel);
    }
}