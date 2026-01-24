using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Features.Details.Page;
using Misa.Ui.Avalonia.Features.Tasks.ListTask;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;
using NavigationViewModel = Misa.Ui.Avalonia.Features.Tasks.Navigation.NavigationViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Page;

public partial class PageViewModel : ViewModelBase, IEntityDetailHost
{
    [ObservableProperty] private Guid _activeEntityId = Guid.Empty;

    public INavigationService NavigationService { get; }

    [ObservableProperty] private TaskDto? _selectedTask;

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        DetailViewModel ??= new DetailPageViewModel(this);
        ActiveEntityId = value?.Id ?? Guid.Empty;
    }

    private ViewModelBase? _currentInfoModel;
    private DetailPageViewModel? DetailViewModel { get; set; }
    public ListViewModel Model { get; }
    public NavigationViewModel Navigation { get; }
    public ObservableCollection<TaskDto> Tasks { get; } = [];

    public async Task AddToCollection(List<TaskDto> tasks)
    {
        foreach (var task in tasks)
        {
            await AddToCollection(task);
        }
    }
    public async Task AddToCollection(TaskDto task)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Tasks.Add(task);
        });
    }

    [ObservableProperty] private string? _pageError;

    public PageViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;

        Model = new ListViewModel(this);
        Navigation = new NavigationViewModel(this);
        
        this.WhenAnyValue(x => x.ActiveEntityId)
            // .Where(id => id != Guid.Empty)
            // .DistinctUntilChanged()
            .Subscribe(_ => ShowDetails());
    }

    public ViewModelBase? CurrentInfoModel
    {
        get => _currentInfoModel;
        set => SetProperty(ref _currentInfoModel, value);
    }

    private void ShowDetails() => CurrentInfoModel = DetailViewModel;
    
}
