using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;

namespace Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Extensions.Sessions;

public partial class SessionViewModel(InspectorOverViewModel parent) : ViewModelBase
{
    private InspectorOverViewModel Parent { get; } = parent;
    [ObservableProperty] private CurrentSessionOverviewDto _currentSessionOverviewDto = null!;
    public bool HasActiveSession => CurrentSessionOverviewDto.ActiveSession != null;
    public bool HasLatestClosedSession => CurrentSessionOverviewDto.LatestClosedSession != null;

    public async Task LoadCurrentSessionAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"items/{Parent.Parent.Item.Id}/overview/session");
        
            var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content
                .ReadFromJsonAsync<Result<CurrentSessionOverviewDto>>(cancellationToken: CancellationToken.None);
        
            if (result?.Value is null)
            {
                return;
            }
            
            CurrentSessionOverviewDto = result.Value;
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
            TimeSpan? plannedDuration = PlannedMinutes.HasValue
                ? TimeSpan.FromMinutes(Convert.ToInt32((object?)PlannedMinutes))
                : null;
            
            var dto = new StartSessionDto(
                Parent.Parent.Item.Id, 
                plannedDuration, 
                Objective,
                StopAutomatically, 
                AutoStopReason
            );
        
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.Item.Id}/sessions/start");
            request.Content = JsonContent.Create(dto);
            
            var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
            
            CloseStartSessionForm();
            await Parent.Parent.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
    
    // Pause Session
    [ObservableProperty] private bool _isPauseSessionFormOpen;
    [ObservableProperty] private string? _pauseReason;
    private void ResetPauseSessionContext()
    {
        PauseReason = null;
    }
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
            var dto = new PauseSessionDto(
                Parent.Parent.Item.Id,
                PauseReason
            );
        
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.Item.Id}/sessions/pause");
            request.Content = JsonContent.Create(dto);
        
            var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            ClosePauseSessionForm();
            await Parent.Parent.Reload();
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
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.Item.Id}/sessions/continue");
        
            var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            await Parent.Parent.Reload();
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
    private void ResetStopSessionContext()
    {
        Summary = null;
    }
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
            var dto = new StopSessionDto(
                Parent.Parent.Item.Id,
                SelectedEfficiency,
                SelectedConcentration,
                Summary
            );
        
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.Item.Id}/sessions/stop");
            request.Content = JsonContent.Create(dto);
        
            var response = await Parent.Parent.NavigationService.NavigationStore
                .HttpClient
                .SendAsync(request, CancellationToken.None);
        
            response.EnsureSuccessStatusCode();
        
            CloseStopSessionForm();
            await Parent.Parent.Reload();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}