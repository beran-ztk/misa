using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorFacadeViewModel : ViewModelBase
{
    private ISelectionContextState ContextState { get; }
    public InspectorGateway Gateway { get; }
    public InspectorState State { get; }
    public InspectorEntryViewModel Entry { get; }
    public PanelProxy PanelProxy { get; }
    public InspectorFacadeViewModel(
        ISelectionContextState  selectionContextState,
        InspectorGateway gateway, 
        InspectorState inspectorState,
        PanelProxy panelProxy)
    {
        ContextState = selectionContextState;
        Gateway = gateway;
        State = inspectorState;
        PanelProxy = panelProxy;
        
        Entry = new InspectorEntryViewModel(this);

        ContextState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContextState.ActiveEntityId))
            {
                _ = GetEntryDataAsync(ContextState.ActiveEntityId);   
            }
        };
    }
    [RelayCommand]
    public async Task Reload()
    {
        await GetEntryDataAsync(State.Item.Id);
    }

    private async Task GetEntryDataAsync(Guid? id)
    {
        // if (id is null) return;
        //
        // var result = await Gateway.GetDetailsAsync((Guid)id);
        // if (result.Value is null) return;
        //
        // State.Item = result.Value.Item;
        // State.Deadline = result.Value.Deadline;
        //
        // var session = await Gateway.GetCurrentAndLatestSessionAsync(State.Item.Id);
        // State.CurrentSessionOverview = session.Value;
    }
}
