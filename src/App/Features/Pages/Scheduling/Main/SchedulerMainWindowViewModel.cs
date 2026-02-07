using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mapping;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Add;
using Misa.Ui.Avalonia.Infrastructure.Services.Interfaces;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using DetailMainWindowViewModel = Misa.Ui.Avalonia.Features.Inspector.Main.DetailMainWindowViewModel;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Main;

public partial class SchedulerMainWindowViewModel : ViewModelBase
{
    private readonly IServiceProvider _sp;
    private readonly IActiveEntitySelection _selection;

    public ObservableCollection<ScheduleDto> Schedules { get; } = [];

    [ObservableProperty] private ScheduleDto? _selectedItem;
    [ObservableProperty] private ViewModelBase? _infoView;
    public INavigationService NavigationService { get; }
    
    private DetailMainWindowViewModel? _detailVm;
    private IDetailCoordinator? _detailCoordinator;
    public SchedulerMainWindowViewModel(
        IServiceProvider sp,
        IActiveEntitySelection selection,
        INavigationService navigationService)
    {
        _sp = sp;
        _selection = selection;
        NavigationService = navigationService;
        
        _ = LoadSchedulesAsync();
    }

    private void EnsureDetail()
    {
        if (_detailVm is not null) return;

        _detailVm = ActivatorUtilities.CreateInstance<DetailMainWindowViewModel>(_sp);
        
        _detailCoordinator = ActivatorUtilities.CreateInstance<DetailCoordinator>(_sp, _detailVm);
        _ = _detailCoordinator.ActivateAsync();
    }

    partial void OnSelectedItemChanged(ScheduleDto? value)
    {
        EnsureDetail();
        
        _selection.SetActive(value?.Id);
        InfoView = _detailVm;
    }
    [RelayCommand]
    private void OpenAddSchedule()
    {
        var addScheduleVm = new AddScheduleViewModel(NavigationService, this);
        InfoView = addScheduleVm;
    }

    private async Task AddToCollection(List<ScheduleDto> schedules)
    {
        foreach (var schedule in schedules)
        {
            await AddToCollection(schedule);
        }
    }
    public async Task AddToCollection(ScheduleDto schedule)
    {
        await Dispatcher.UIThread.InvokeAsync(() => 
        {
            Schedules.Add(schedule);
        });
    }
    private async Task LoadSchedulesAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "scheduling");
            
            using var response = await NavigationService.NavigationStore.HttpClient
                .SendAsync(request, CancellationToken.None);

            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<List<ScheduleDto>>>(cancellationToken: CancellationToken.None);

            Schedules.Clear();
            await AddToCollection(result?.Value ?? []);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}