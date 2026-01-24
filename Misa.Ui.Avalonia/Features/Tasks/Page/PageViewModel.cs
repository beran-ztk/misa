using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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
    [ObservableProperty] private TaskDto? _selectedTask;
    public INavigationService NavigationService { get; }
    private DetailPageViewModel? DetailViewModel { get; set; }
    private readonly IServiceProvider _services;
    public PageViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        _services = navigationService.ServiceProvider;

        Model = new ListViewModel(this);
        Navigation = new NavigationViewModel(this);
        
        this.WhenAnyValue(x => x.ActiveEntityId)
            .Subscribe(_ => ShowDetails());
    }

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        DetailViewModel ??= CreateDetailVm();
        ActiveEntityId = value?.Id ?? Guid.Empty;
        _ = DetailViewModel.LoadAsync(value?.Id ?? Guid.Empty);
    }
    private DetailPageViewModel CreateDetailVm() => 
        ActivatorUtilities.CreateInstance<DetailPageViewModel>(_services, this);
    
    private ViewModelBase? _currentInfoModel;
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

    

    public ViewModelBase? CurrentInfoModel
    {
        get => _currentInfoModel;
        set => SetProperty(ref _currentInfoModel, value);
    }

    private void ShowDetails() => CurrentInfoModel = DetailViewModel;
    
}
