using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Features.Tasks.Add;
using Misa.Ui.Avalonia.Presentation.Mapping;
using TaskMainWindowViewModel = Misa.Ui.Avalonia.Features.Tasks.Main.TaskMainWindowViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Header;

public partial class TaskHeaderViewModel(TaskMainWindowViewModel vm) : ViewModelBase
{
    private TaskMainWindowViewModel Parent { get; } = vm;
    [ObservableProperty] private string _searchText = string.Empty;
    partial void OnSearchTextChanged(string value) => Parent.ApplyFilters();
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
            await Parent.CreateTask(result);
    }
}