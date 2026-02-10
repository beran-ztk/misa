using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Contract.Shared.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Common;
using Misa.Ui.Avalonia.Features.Inspector.Features.Overview.Base;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Infrastructure.States;

namespace Misa.Ui.Avalonia.Features.Inspector.Base;

public partial class InspectorViewModel : ViewModelBase
{
    [ObservableProperty] private int _selectedTabIndex;

    [ObservableProperty] private ItemDto _item;
    [ObservableProperty] private DeadlineDto _deadline;
    [ObservableProperty] private IItemExtensionVm? _extension;


    private readonly IInspectorItemExtensionVmFactory _extensionFactory;
    public InspectorOverViewModel InspectorOverViewModel { get; }
    private ISelectionContextState ContextState { get; }
    private readonly InspectorGateway _gateway;
    public readonly InspectorState State;
    public HttpClient HttpClient { get; }
    public InspectorViewModel(
        ISelectionContextState  selectionContextState,
        InspectorGateway gateway, 
        InspectorState inspectorState,
        IInspectorItemExtensionVmFactory extensionFactory, 
        HttpClient httpClient)
    {
        ContextState = selectionContextState;
        _gateway = gateway;
        State = inspectorState;
        _extensionFactory = extensionFactory;
        HttpClient = httpClient;
        
        Item = ItemDto.Empty();
        InspectorOverViewModel = new InspectorOverViewModel(this);

        ContextState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContextState.ActiveEntityId))
                _ = LoadAsync(ContextState.ActiveEntityId, CancellationToken.None);
        };
    }

    public Task Clear()
    {
        Item = ItemDto.Empty();
        InspectorOverViewModel.Description.Descriptions.Clear();
        Extension = null;
        SelectedTabIndex = 0;
        return Task.CompletedTask;
    }

    public async Task Reload()
    {
        await LoadAsync(Item.Id, CancellationToken.None);
    }
    public async Task LoadAsync(Guid? itemId, CancellationToken ct)
    {
        if (itemId is null) return;
        var result = await _gateway.GetDetailsAsync((Guid)itemId);
        
        if (result is null)
        {
            await Clear();
            return;
        }
        
        Item = result.Item;
        Deadline = result.Deadline;
        Extension = _extensionFactory.Create(result);

        InspectorOverViewModel.Description.Load();
        
        await InspectorOverViewModel.Session.LoadCurrentSessionAsync();
    }
}
