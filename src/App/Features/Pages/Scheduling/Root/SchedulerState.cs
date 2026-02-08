using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Add;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerState(
    ISelectionContextState selectionContextState, 
    HttpClient httpClient,
    AddScheduleViewModel addScheduleViewModel,
    UserState userState)
    : ObservableObject
{
    public AddScheduleViewModel AddScheduleViewModel { get; } = addScheduleViewModel;

    public ObservableCollection<ScheduleDto> Schedules { get; } = [];

    [ObservableProperty] private ScheduleDto? _selectedItem;
    partial void OnSelectedItemChanged(ScheduleDto? value)
    {
        selectionContextState.SetActive(value?.Id);
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
    public async Task LoadSchedulesAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "scheduling");
            
            using var response = await httpClient.SendAsync(request, CancellationToken.None);

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
    public async Task CreateSchedule(AddScheduleDto dto)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"scheduling/{userState.User.Id}");
            request.Content = JsonContent.Create(dto);

            using var response = await httpClient.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<Result<ScheduleDto>>(CancellationToken.None);
            if (created?.Value != null)
            {
                await AddToCollection(created.Value);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}