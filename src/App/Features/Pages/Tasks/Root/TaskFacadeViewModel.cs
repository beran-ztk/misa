using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel : ViewModelBase
{
    private readonly TaskGateway _gateway;
    private readonly LayerProxy _layerProxy;

    public TaskState State { get; }

    public TaskFacadeViewModel(
        TaskState state,
        TaskGateway gateway,
        LayerProxy layerProxy)
    {
        State = state;
        _gateway = gateway;
        _layerProxy = layerProxy;
        
        
        State.SelectionContextState.PropertyChanged += async (s, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(State.SelectionContextState.UpdatedVersion):
                {
                    var id = State.SelectionContextState.ActiveEntityId;
                    await GetAllAsync();
                    State.SelectionContextState.Set(id);
                    break;
                }
                case nameof(State.SelectionContextState.RemovedVersion):
                    await GetAllAsync();
                    break;
            }
        };
    }

    public async Task InitializeWorkspaceAsync()
    {
        await RefreshWorkspaceAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var values = await _gateway.GetAllAsync();
        await State.SetMainCollection(values);
    }
    
    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateTaskViewModel(_gateway);

        var created = await _layerProxy.OpenAsync<CreateTaskViewModel, TaskDto>(formVm);
        if (created is null) return;

        await State.AppendToMainCollection(created);
    }
}