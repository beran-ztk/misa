using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
     public SessionResolvedDto? CurrentSession => Facade.State.CurrentSessionOverview?.ActiveSession;
     
     public bool HasActiveSession => CurrentSession != null;

     public bool CanStartSession => CurrentSession == null;
     public bool CanPauseSession => CurrentSession?.State == SessionStateDto.Running;
     public bool CanContinueSession => CurrentSession?.State == SessionStateDto.Paused;
     public bool CanEndSession => CurrentSession != null;
     
    public string ActiveSessionSegmentDisplay => $"Segment {CurrentSession?.Segments.Count} - {CurrentSession?.State}";
    
    [RelayCommand]
    private async Task ShowStartSessionPanelAsync()
    {
        var itemId = Facade.State.Item.Id;

        var formVm = new StartSessionViewModel(itemId, Facade.Gateway);

        var result = await Facade.PanelProxy.OpenAsync(Panels.StartSession, formVm);

        if (Facade.State.CurrentSessionOverview is not null)
        {
            Facade.State.CurrentSessionOverview.ActiveSession = result;
            CurrentSessionPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task ShowPauseSessionPanelAsync()
    {
        var itemId = Facade.State.Item.Id;

        var formVm = new PauseSessionViewModel(itemId, Facade.Gateway);

        var result = await Facade.PanelProxy.OpenAsync(Panels.PauseSession, formVm);

        if (Facade.State.CurrentSessionOverview is not null)
        {
            Facade.State.CurrentSessionOverview.ActiveSession = result;
            CurrentSessionPropertyChanged();
        }
    }
    
    [RelayCommand]
    private async Task ShowEndSessionPanelAsync()
    {
        var itemId = Facade.State.Item.Id;

        var formVm = new EndSessionViewModel(itemId, Facade.Gateway);

        var result = await Facade.PanelProxy.OpenAsync(PanelKey.EndSession, formVm);

        if (result is { IsSuccess: true } && Facade.State.CurrentSessionOverview is not null)
        {
            Facade.State.CurrentSessionOverview.ActiveSession = null;
        }
        
        CurrentSessionPropertyChanged();
    }
    [RelayCommand]
    private async Task ContinueSessionAsync()
    {
        var itemId = Facade.State.Item.Id;

        await Facade.Gateway.ContinueSessionAsync(itemId);
        await Facade.Reload();
    }
}
