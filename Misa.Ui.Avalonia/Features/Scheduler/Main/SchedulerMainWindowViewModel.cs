using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Infrastructure.Services.Navigation;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Scheduler.Main;

public partial class SchedulerMainWindowViewModel : ViewModelBase
{
    private INavigationService NavigationService { get; }
    private ObservableCollection<ScheduleDto> Schedules { get; } = [];

    private ViewModelBase? _infoView;

    /// <inheritdoc/>
    public SchedulerMainWindowViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        
        _ = LoadSchedulesAsync();
    }

    public ViewModelBase? InfoView
    {
        get => _infoView;
        set => SetProperty(ref _infoView, value);
    }

    [RelayCommand]
    private void OpenAddSchedule()
    {
        var addScheduleVm = new Add.AddScheduleViewModel(NavigationService);
        InfoView = addScheduleVm;
    }

    private async Task AddToCollection(List<ScheduleDto> schedules)
    {
        foreach (var schedule in schedules)
        {
            await AddToCollection(schedule);
        }
    }
    private async Task AddToCollection(ScheduleDto schedule)
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
            
            using var response = await NavigationService.NavigationStore.MisaHttpClient
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