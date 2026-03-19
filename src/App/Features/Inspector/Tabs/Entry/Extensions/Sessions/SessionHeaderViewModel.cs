using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Features.Utilities.Toast;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    public SessionDto? CurrentSession =>
        Facade.State.Item.Activity?.Sessions.FirstOrDefault(s => s.State != SessionStateDto.Ended);
     
     public bool HasActiveSession => CurrentSession != null;

     public bool CanStartSession    => CurrentSession == null                             && Facade.State.CanManageSessions;
     public bool CanPauseSession    => CurrentSession?.State == SessionStateDto.Running    && Facade.State.CanManageSessions;
     public bool CanContinueSession => CurrentSession?.State == SessionStateDto.Paused     && Facade.State.CanManageSessions;
     public bool CanEndSession      => CurrentSession != null                              && Facade.State.CanManageSessions;
     
    public bool IsRunning => CurrentSession?.State == SessionStateDto.Running;
    public bool IsPaused  => CurrentSession?.State == SessionStateDto.Paused;

    public string SessionStateLabel => IsRunning ? "RUNNING" : "PAUSED";

    public string? ActiveSessionObjective     => CurrentSession?.Objective;
    public bool    HasActiveSessionObjective  => !string.IsNullOrWhiteSpace(ActiveSessionObjective);

    public string ActiveSessionSegmentLabel => $"Seg {CurrentSession?.Segments.Count}";

    public string ActiveSessionSegmentDisplay => $"Segment {CurrentSession?.Segments.Count} - {CurrentSession?.State}";

    
    [RelayCommand]
    private async Task ShowStartSessionPanelAsync()
    {
        var formVm = new StartSessionViewModel(Facade.State.Item.Id, Facade.Gateway);
        
        var result = await Facade.LayerProxy.OpenAsync<StartSessionViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
        {
            await Facade.Reload();
            Facade.LayerProxy.ShowActionToast("Session started", type: ToastType.Success);
        }
    }

    [RelayCommand]
    private async Task ShowPauseSessionPanelAsync()
    {
        if (CurrentSession == null)
            return;
        
        var formVm = new PauseSessionViewModel(Facade.State.Item.Id, Facade.Gateway);
        
        var result = await Facade.LayerProxy.OpenAsync<PauseSessionViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
        {
            await Facade.Reload();
            Facade.LayerProxy.ShowActionToast("Session paused", type: ToastType.Info);
        }
    }
    
    [RelayCommand]
    private async Task ShowEndSessionPanelAsync()
    {
        var formVm = new EndSessionViewModel(Facade.State.Item.Id, Facade.Gateway);
        
        var result = await Facade.LayerProxy.OpenAsync<EndSessionViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
        {
            await Facade.Reload();
            Facade.LayerProxy.ShowActionToast("Session stopped", type: ToastType.Info);
        }
    }
    [RelayCommand]
    private async Task ContinueSessionAsync()
    {
        var itemId = Facade.State.Item.Id;

        var result = await Facade.Gateway.ContinueSessionAsync(itemId);
        if (result is { IsSuccess: true })
        {
            await Facade.Reload();
        }
    }
}
