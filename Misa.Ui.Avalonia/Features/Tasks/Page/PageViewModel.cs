using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Features.Details.Page;
using Misa.Ui.Avalonia.Features.Tasks.ListTask;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;
using ReactiveUI;
using NavigationViewModel = Misa.Ui.Avalonia.Features.Tasks.Navigation.NavigationViewModel;

namespace Misa.Ui.Avalonia.Features.Tasks.Page;

public partial class PageViewModel : ViewModelBase, IEntityDetailHost, IDisposable
{
    [ObservableProperty] private Guid _activeEntityId = Guid.Empty;

    public INavigationService NavigationService { get; }

    [ObservableProperty] private ListTaskDto? _selectedTask;

    partial void OnSelectedTaskChanged(ListTaskDto? value)
    {
        DetailViewModel ??= new DetailPageViewModel(this);
        ActiveEntityId = value?.Id ?? Guid.Empty;
    }

    private ViewModelBase? _currentInfoModel;
    private DetailPageViewModel? DetailViewModel { get; set; }

    public ListViewModel Model { get; }
    public NavigationViewModel Navigation { get; }
    public ObservableCollection<ListTaskDto> Tasks { get; } = [];

    [ObservableProperty] private string? _pageError;


    private readonly IDisposable _subOpenCreate;
    private readonly IDisposable _subCloseRight;
    private readonly IDisposable _subReload;
    private readonly IDisposable _subCreated;
    private readonly IDisposable _subCreateFailed;

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

    public void ShowDetails() => CurrentInfoModel = DetailViewModel;

    public void Dispose()
    {
        _subOpenCreate.Dispose();
        _subCloseRight.Dispose();
        _subReload.Dispose();
        _subCreated.Dispose();
        _subCreateFailed.Dispose();

        DetailViewModel.Dispose();
    }
}
