using Misa.Ui.Avalonia.Features.Tasks.Page;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Tasks.Navigation;

public class NavigationViewModel : ViewModelBase
{
    public PageViewModel MainViewModel { get; }

    public NavigationViewModel(PageViewModel vm)
    {
        MainViewModel = vm;
    }
}