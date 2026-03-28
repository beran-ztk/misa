using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Activity;
using Misa.Ui.Avalonia.Features.Inspector.Tabs.Entry.Base;
using Misa.Ui.Avalonia.Infrastructure;

namespace Misa.Ui.Avalonia.Features.Inspector;

public sealed partial class InspectorFacadeViewModel : ViewModelBase
{
    public ISelectionContextState ContextState { get; }
    public InspectorState State { get; }
    public InspectorEntryViewModel Entry { get; }
    public InspectorActivityViewModel Activity { get; }
    public InspectorFacadeViewModel(
        ISelectionContextState  selectionContextState,
        InspectorState inspectorState)
    {
        ContextState = selectionContextState;
        State = inspectorState;
        
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
        
        // var result = await Gateway.GetItemAsync((Guid)id);
        // if (result.Value is null) return;
        
        // State.Item = result.Value;
    }
}
