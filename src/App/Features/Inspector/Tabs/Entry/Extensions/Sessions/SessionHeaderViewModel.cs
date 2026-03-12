using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
    public SessionDto? CurrentSession =>
        Facade.State.Item.Activity?.Sessions.FirstOrDefault(s => s.State != SessionStateDto.Ended);
     
     public bool HasActiveSession => CurrentSession != null;

     public bool CanStartSession => CurrentSession == null;
     public bool CanPauseSession => CurrentSession?.State == SessionStateDto.Running;
     public bool CanContinueSession => CurrentSession?.State == SessionStateDto.Paused;
     public bool CanEndSession => CurrentSession != null;
     
    public string ActiveSessionSegmentDisplay => $"Segment {CurrentSession?.Segments.Count} - {CurrentSession?.State}";

    
    [RelayCommand]
    private async Task ShowStartSessionPanelAsync()
    {
        var formVm = new StartSessionViewModel(Facade.State.Item.Id, Facade.Gateway);
        
        var result = await Facade.LayerProxy.OpenAsync<StartSessionViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
        {
            await Facade.Reload();
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
