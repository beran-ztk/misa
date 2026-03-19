using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Root;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Activity;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;
using Misa.Ui.Avalonia.Infrastructure.States;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Inspector;

public sealed partial class InspectorFacadeViewModel : ViewModelBase
{
    public ISelectionContextState ContextState { get; }
    public InspectorGateway Gateway { get; }
    public InspectorState State { get; }
    public InspectorEntryViewModel Entry { get; }
    public InspectorActivityViewModel Activity { get; }
    public LayerProxy LayerProxy { get; }
    public InspectorFacadeViewModel(
        ISelectionContextState  selectionContextState,
        InspectorGateway gateway, 
        InspectorState inspectorState,
        LayerProxy layerProxy)
    {
        ContextState = selectionContextState;
        Gateway = gateway;
        State = inspectorState;
        LayerProxy = layerProxy;
        
        Entry = new InspectorEntryViewModel(this);
        Activity = new InspectorActivityViewModel(this);

        ContextState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ContextState.ActiveEntityId))
            {
                _ = GetEntryDataAsync(ContextState.ActiveEntityId);   
            }
        };
    }
    [RelayCommand]
    private void SelectEntryTab() => State.SelectedTabIndex = 0;

    [RelayCommand]
    private void SelectActivityTab() => State.SelectedTabIndex = 1;

    [RelayCommand]
    public async Task Reload()
    {
        await GetEntryDataAsync(State.Item?.Id);
    }

    private async Task GetEntryDataAsync(Guid? id)
    {
        if (id is null) return;
        
        var result = await Gateway.GetItemAsync((Guid)id);
        if (result.Value is null) return;
        
        State.Item = result.Value;
    }
}
