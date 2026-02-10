using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Common;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Inspector.Base;

public sealed partial class InspectorViewModel : ViewModelBase
{
    private readonly IInspectorItemExtensionVmFactory _extensionFactory;
    public InspectorOverViewModel InspectorOverViewModel { get; }
    private ISelectionContextState ContextState { get; }
    
    public InspectorGateway Gateway { get; }

    public InspectorState State { get; }
    public HttpClient HttpClient { get; }
    public InspectorViewModel(
        ISelectionContextState  selectionContextState,
        InspectorGateway gateway, 
        InspectorState inspectorState,
        IInspectorItemExtensionVmFactory extensionFactory, 
        HttpClient httpClient)
    {
        ContextState = selectionContextState;
        Gateway = gateway;
        State = inspectorState;
        _extensionFactory = extensionFactory;
        HttpClient = httpClient;
        
        State.Item = ItemDto.Empty();
        InspectorOverViewModel = new InspectorOverViewModel(this);

        ContextState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContextState.ActiveEntityId))
                _ = LoadAsync(ContextState.ActiveEntityId, CancellationToken.None);
        };
    }

    public void Clear()
    {
        State.Item = ItemDto.Empty();
        InspectorOverViewModel.Description.Descriptions.Clear();
        State.Extension = null;
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
        State.Extension = _extensionFactory.Create(result);

        InspectorOverViewModel.Description.Load();
        
        await InspectorOverViewModel.Session.LoadCurrentSessionAsync();
    }
}
