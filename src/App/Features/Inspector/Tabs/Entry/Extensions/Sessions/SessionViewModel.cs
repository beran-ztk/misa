using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions;

public partial class SessionViewModel(InspectorFacadeViewModel facade) : ViewModelBase
{
    private InspectorFacadeViewModel Facade { get; } = facade;

    public CurrentSessionOverviewDto? CurrentSessionOverviewDto
        => Facade.State.CurrentSessionOverview;

    public bool HasActiveSession => CurrentSessionOverviewDto?.ActiveSession != null;
    public bool HasLatestClosedSession => CurrentSessionOverviewDto?.LatestClosedSession != null;

    public async Task LoadCurrentSessionAsync()
    {
        try
        {
            var itemId = Facade.State.Item.Id;

            var dto = await Facade.Gateway.GetCurrentSessionOverviewAsync(itemId);

            Facade.State.CurrentSessionOverview = dto;
            OnPropertyChanged(nameof(CurrentSessionOverviewDto));
            OnPropertyChanged(nameof(HasActiveSession));
            OnPropertyChanged(nameof(HasLatestClosedSession));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    // Start Session
    [ObservableProperty] private bool _isStartSessionFormOpen;

    [ObservableProperty] private int? _plannedMinutes;
    [ObservableProperty] private string? _objective;
    [ObservableProperty] private bool _stopAutomatically;
    [ObservableProperty] private string? _autoStopReason;

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
