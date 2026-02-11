using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Extensions.Sessions.Forms;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;

public partial class InspectorEntryViewModel
{
     public CurrentSessionOverviewDto? CurrentSession => Facade.State.CurrentSessionOverview;

    public bool HasActiveSession => CurrentSession?.ActiveSession != null;
    
    public string ActiveSessionElapsedDisplay
        => CurrentSession?.ActiveSession?.ElapsedTime ?? string.Empty;

    public string ActiveSessionSegmentDisplay => $"Segment {CurrentSession?.ActiveSession?.Segments.Count}";
    
    [RelayCommand]
    public async Task ShowStartSessionPanelAsync()
    {
        var itemId = Facade.State.Item.Id;
        
        var formVm = new StartSessionViewModel(itemId);
        
        var dto = await Facade.PanelProxy.OpenAsync<StartSessionDto>(PanelKey.StartSession, formVm);
        if (dto is null) return;
        
        await Facade.Gateway.StartSessionAsync(itemId, dto);
        await Facade.Reload();
    }
}
