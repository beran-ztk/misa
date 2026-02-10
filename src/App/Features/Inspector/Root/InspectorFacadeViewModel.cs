using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Inspector.Root;

public sealed partial class InspectorFacadeViewModel : ViewModelBase
{
    private ISelectionContextState ContextState { get; }
    
    public InspectorGateway Gateway { get; }

    public InspectorState State { get; }
    public InspectorFacadeViewModel(
        ISelectionContextState  selectionContextState,
        InspectorGateway gateway, 
        InspectorState inspectorState)
    {
        ContextState = selectionContextState;
        Gateway = gateway;
        State = inspectorState;
        
        State.Item = ItemDto.Empty();

        ContextState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContextState.ActiveEntityId))
                _ = LoadAsync(ContextState.ActiveEntityId, CancellationToken.None);
        };
    }

    public void Clear()
    {
        State.Item = ItemDto.Empty();
        State.SelectedTabIndex = 0;
        
        State.CurrentSessionOverview = null;
    }
    public async Task Reload()
    {
        await LoadAsync(State.Item.Id, CancellationToken.None);
    }
    public async Task LoadAsync(Guid? itemId, CancellationToken ct)
    {
        if (itemId is null) return;
        var result = await Gateway.GetDetailsAsync((Guid)itemId);
        
        if (result is null)
        {
            Clear();
            return;
        }
        State.Item = result.Item;
        State.Deadline = result.Deadline;
        
        await InspectorOverViewModel.Session.LoadCurrentSessionAsync();
    }
}
