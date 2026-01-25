using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Scheduler.Main;

public partial class SchedulerMainWindowViewModel(INavigationService navigationService) : ViewModelBase
{
    private INavigationService NavigationService { get; } = navigationService;

    private ViewModelBase? _currentView;
    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    [RelayCommand]
    private void OpenAddSchedule()
    {
        var addScheduleVm = new Add.AddScheduleViewModel(NavigationService);
        CurrentView = addScheduleVm;
    }
}