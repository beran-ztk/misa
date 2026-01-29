using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Features.Tasks.Add;
using Misa.Ui.Avalonia.Presentation.Mapping;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Header;

public partial class TaskHeaderViewModel(TaskMainWindowViewModel vm) : ViewModelBase
{
    private TaskMainWindowViewModel Parent { get; } = vm;

    [RelayCommand]
    private void ShowAddItemForm() => Parent.InfoView = new AddTaskViewModel(Parent);
}