using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Audit.Session;
using Misa.Ui.Avalonia.Presentation.Mapping;

namespace Misa.Ui.Avalonia.Features.Details.Information.Extensions.Sessions;

public partial class SessionViewModel : ViewModelBase
{
    private InformationViewModel Parent { get; }
    
    public SessionViewModel(InformationViewModel parent)
    {
        Parent = parent;
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
                ? TimeSpan.FromMinutes(Convert.ToInt32(PlannedMinutes))
                : null;
            
            var dto = new StartSessionDto(
                Parent.Parent.ItemOverview.Item.Id, 
                plannedDuration, 
                Objective,
                StopAutomatically, 
                AutoStopReason
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.ItemOverview.Item.Id}/sessions/start");
            request.Content = JsonContent.Create(dto);
            
            var response = await Parent.Parent.EntityDetailHost.NavigationService.NavigationStore
                .MisaHttpClient
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
                Parent.Parent.ItemOverview.Item.Id,
                PauseReason
            );

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"items/{Parent.Parent.ItemOverview.Item.Id}/sessions/pause");
            request.Content = JsonContent.Create(dto);

            var response = await Parent.Parent.EntityDetailHost.NavigationService.NavigationStore
                .MisaHttpClient
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
    // [RelayCommand]
    // private async Task SessionContinue()
    // {
    //     try
    //     {
    //         var response = await Parent.Parent.EntityDetailHost.NavigationService.NavigationStore
    //             .MisaHttpClient.PostAsync(
    //                 requestUri: $"Sessions/Continue/{Parent.Parent.ItemOverview.Item.Id}",
    //                 content: null
    //             );
    //
    //
    //         if (!response.IsSuccessStatusCode)
    //             Console.WriteLine($"Server returned {response.StatusCode}: {response.ReasonPhrase}");
    //
    //         await Parent.Parent.Reload();
    //     }
    //     catch (Exception e)
    //     {
    //         Console.WriteLine(e);
    //     }
    // }
}