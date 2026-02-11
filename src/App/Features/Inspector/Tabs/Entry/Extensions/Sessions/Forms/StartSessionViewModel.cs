using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    // Start Session
    [ObservableProperty] private bool _isStartSessionFormOpen;

    [ObservableProperty] private int? _plannedMinutes;
    [ObservableProperty] private string? _objective;
    [ObservableProperty] private bool _stopAutomatically;
    [ObservableProperty] private string? _autoStopReason;

    [RelayCommand]
    public async Task ShowAddPanelAsync()
    {
        var formVm = new CreateScheduleViewModel(State.CreateState);

        var dto = await PanelProxy.OpenAsync<AddScheduleDto>(PanelKey.Schedule, formVm);
        if (dto is null) return; // Cancel/X

        var item = await Gateway.CreateAsync(dto);
        await State.AddToCollection(item);
    }
    private void ResetStartSessionContext()
    {
        PlannedMinutes = null;
        Objective = null;
        StopAutomatically = false;
        AutoStopReason = null;
    }

    [RelayCommand]
    private void ShowStartSessionForm()
    {
        IsStartSessionFormOpen = true;
        ResetStartSessionContext();
    }

    [RelayCommand]
    private void CloseStartSessionForm()
    {
        IsStartSessionFormOpen = false;
        ResetStartSessionContext();
    }

    [RelayCommand]
    private async Task StartSession()
    {
        try
        {
            var itemId = Facade.State.Item.Id;

            TimeSpan? plannedDuration = PlannedMinutes.HasValue
                ? TimeSpan.FromMinutes(Convert.ToInt32(PlannedMinutes))
                : null;

            var dto = new StartSessionDto(
                itemId,
                plannedDuration,
                Objective,
                StopAutomatically,
                AutoStopReason
            );

            await Facade.Gateway.StartSessionAsync(itemId, dto);

            CloseStartSessionForm();
            await Facade.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}