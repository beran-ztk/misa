using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Features.Tasks.Add;
using Misa.Ui.Avalonia.Features.Tasks.Page;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Tasks.Navigation;

public partial class NavigationViewModel(PageViewModel vm) : ViewModelBase
{
    private PageViewModel Parent { get; } = vm;

    [RelayCommand]
    private void ShowAddItemForm() => Parent.InfoView = new AddTaskViewModel(Parent);
}