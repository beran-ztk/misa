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
    public TaskViewModel(NavigationStore navigationStore)
    {
        NavigationStore = navigationStore;
        
        _httpClient = NavigationStore.MisaHttpClient;
        ListModel = new TaskListViewModel(this, navigationStore);
        Navigation = new TaskNavigationViewModel(this, NavigationStore);
    }
    private readonly HttpClient _httpClient;
    public TaskListViewModel ListModel { get; }
    public TaskNavigationViewModel Navigation { get; }
    public NavigationStore NavigationStore { get; }
    public ObservableCollection<ReadItemDto> Items { get; set; } = [];
    
    private ReadItemDto? _selectedEntity;
    private readonly INavigationService _navigationService;
    
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