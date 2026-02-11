using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    // Pause Session
    [ObservableProperty] private bool _isPauseSessionFormOpen;
    [ObservableProperty] private string? _pauseReason;

    private void ResetPauseSessionContext() => PauseReason = null;

    [RelayCommand]
    private void ShowPauseSessionForm()
    {
        IsPauseSessionFormOpen = true;
        ResetPauseSessionContext();
    }

    [RelayCommand]
    private void ClosePauseSessionForm()
    {
        IsPauseSessionFormOpen = false;
        ResetPauseSessionContext();
    }

    [RelayCommand]
    private async Task PauseSession()
    {
        try
        {
            var itemId = Facade.State.Item.Id;

            var dto = new PauseSessionDto(itemId, PauseReason);

            await Facade.Gateway.PauseSessionAsync(itemId, dto);

            ClosePauseSessionForm();
            await Facade.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    // Continue Session
    [RelayCommand]
    private async Task ContinueSession()
    {
        try
        {
            var itemId = Facade.State.Item.Id;

            await Facade.Gateway.ContinueSessionAsync(itemId);

            await Facade.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    // Stop Session
    [ObservableProperty] private bool _isStopSessionFormOpen;

    [ObservableProperty] private string? _summary;
    [ObservableProperty] private EfficiencyContract _selectedEfficiency;
    [ObservableProperty] private ConcentrationContract _selectedConcentration;

    public IReadOnlyList<EfficiencyContract> Efficiencies { get; } = Enum.GetValues<EfficiencyContract>();
    public IReadOnlyList<ConcentrationContract> Concentrations { get; } = Enum.GetValues<ConcentrationContract>();

    private void ResetStopSessionContext() => Summary = null;

    [RelayCommand]
    private void ShowStopSessionForm()
    {
        IsStopSessionFormOpen = true;
        ResetStopSessionContext();
    }

    [RelayCommand]
    private void CloseStopSessionForm()
    {
        IsStopSessionFormOpen = false;
        ResetStopSessionContext();
    }

    [RelayCommand]
    private async Task StopSession()
    {
        try
        {
            var itemId = Facade.State.Item.Id;

            var dto = new StopSessionDto(
                itemId,
                SelectedEfficiency,
                SelectedConcentration,
                Summary
            );

            await Facade.Gateway.StopSessionAsync(itemId, dto);

            CloseStopSessionForm();
            await Facade.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
