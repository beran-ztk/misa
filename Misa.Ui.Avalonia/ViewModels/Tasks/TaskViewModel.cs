using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using Avalonia.Controls;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Services.Navigation;
using Misa.Ui.Avalonia.Stores;
using Misa.Ui.Avalonia.ViewModels.Entities;
using Misa.Ui.Avalonia.ViewModels.Items;
using Misa.Ui.Avalonia.ViewModels.Shells;
using ReactiveUI;

namespace Misa.Ui.Avalonia.ViewModels.Tasks;

public enum TaskDetailMode
{
    None,
    Create,
    View,
    Edit
}
public class TaskViewModel : ViewModelBase
{
    public TaskViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        
        ListModel = new TaskListViewModel(this);
        Navigation = new TaskNavigationViewModel(this);
    }
    public INavigationService NavigationService;
    public TaskListViewModel ListModel { get; }
    public TaskNavigationViewModel Navigation { get; }
    public ObservableCollection<ReadItemDto> Items { get; set; } = [];
    
    private ReadItemDto? _selectedEntity;
    
    private ViewModelBase? _currentInfoModel;
    public ViewModelBase? CurrentInfoModel
    {
        get => _currentInfoModel;
        set => SetProperty(ref _currentInfoModel, value);
    }
    public ReadItemDto? SelectedEntity
    {
        get => _selectedEntity;
        set => SetProperty(ref _selectedEntity, value);
    }
}