using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Services.Navigation;

namespace Misa.Ui.Avalonia.ViewModels.Shells;

public partial class NavigationViewModel : ViewModelBase
{
    public NavigationViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }
    private readonly INavigationService _navigationService;
    
    [RelayCommand]
    public void ShowTasks()
    {
        _navigationService.ShowTasks();
    }
}