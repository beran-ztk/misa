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

public partial class PageViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly IActiveEntitySelection _selection;
    
    public ObservableCollection<TaskDto> Tasks { get; } = [];
    
    [ObservableProperty] private TaskDto? _selectedTask;
    [ObservableProperty] private ViewModelBase? _infoView;
    public INavigationService NavigationService { get; }
    
    private DetailPageViewModel? _detailVm;
    private IDetailCoordinator? _detailCoordinator;
    
    public ListViewModel Model { get; }
    public NavigationViewModel Navigation { get; }
    [ObservableProperty] private string? _pageError;
    public PageViewModel(
        IServiceProvider sp,
        IActiveEntitySelection selection,
        INavigationService navigationService)
    {
        _sp = sp;
        _selection = selection;
        NavigationService = navigationService;
        
        Model = new ListViewModel(this);
        Navigation = new NavigationViewModel(this);
    }
    private void EnsureDetail()
    {
        if (_detailVm is not null) return;

        _detailVm = ActivatorUtilities.CreateInstance<DetailPageViewModel>(_sp);
        
        _detailCoordinator = ActivatorUtilities.CreateInstance<DetailCoordinator>(_sp, _detailVm);
        _ = _detailCoordinator.ActivateAsync();
    }
    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        EnsureDetail();
        
        _selection.SetActive(value?.Id);
        InfoView = _detailVm;
    }

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

}
