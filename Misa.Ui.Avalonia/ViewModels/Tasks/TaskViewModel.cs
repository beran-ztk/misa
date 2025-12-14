using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Entities;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Interfaces;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.ViewModels.Details;
using Misa.Ui.Avalonia.ViewModels.Shells;

namespace Misa.Ui.Avalonia.ViewModels.Tasks;

public enum TaskDetailMode
{
    None,
    Create,
    View,
    Edit
}
public partial class TaskViewModel : ViewModelBase, IEntityDetail
{
    private ReadEntityDto? _selectedEntity;
    
    private ReadItemDto? _selectedTask;
    
    private ViewModelBase? _currentInfoModel;
    public INavigationService NavigationService;
    public DetailMainDetailViewModel DetailViewModel { get; } 
    public TaskListViewModel ListModel { get; }
    public TaskNavigationViewModel Navigation { get; }
    public ObservableCollection<ReadItemDto> Items { get; set; } = [];
    
    [ObservableProperty] private bool _isCreateTaskFormOpen;
    public TaskViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        
        ListModel = new TaskListViewModel(this);
        Navigation = new TaskNavigationViewModel(this);
        DetailViewModel = new DetailMainDetailViewModel(this, NavigationService);
    }
    
    public ViewModelBase? CurrentInfoModel
    {
        get => _currentInfoModel;
        set => SetProperty(ref _currentInfoModel, value);
    }
    public ReadEntityDto? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            SetProperty(ref _selectedEntity, value);
            ShowDetails();
        }
    }
    public void ShowDetails() => CurrentInfoModel = DetailViewModel;
    public ReadItemDto? SelectedTask
    {
        get => _selectedTask;
        set
        {
            SetProperty(ref _selectedTask, value);
            SelectedEntity = value?.Entity;
        }
    }
}