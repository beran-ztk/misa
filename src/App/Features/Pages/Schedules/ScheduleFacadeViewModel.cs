using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Schedules;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules;

public sealed partial class ScheduleFacadeViewModel : ViewModelBase
{
    public ScheduleState State { get; }
    private LayerProxy LayerProxy { get; }

    public ScheduleFacadeViewModel(ScheduleState state,
        LayerProxy layerProxy)
    {
        State = state;
        LayerProxy = layerProxy;
        
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
        // var result = await Gateway.GetAllAsync();
        // await State.AddToCollection(result);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateScheduleViewModel();
        
        var created = await LayerProxy.OpenAsync<CreateScheduleViewModel, ScheduleDto>(formVm);
        if (created is null) return;
        
        await State.AddToCollection(created);
    }

}
