using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Schedules.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schedules.Root;

public sealed partial class ScheduleFacadeViewModel(
    ScheduleState state,
    ScheduleGateway gateway,
    LayerProxy layerProxy)
    : ViewModelBase
{
    public ScheduleState State { get; } = state;
    private ScheduleGateway Gateway { get; } = gateway;
    private LayerProxy LayerProxy { get; } = layerProxy;

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
        var result = await Gateway.GetAllAsync();
        await State.AddToCollection(result);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        State.CreateState.Reset();

        // var formVm = new CreateScheduleViewModel(State.CreateState, Gateway);
        //
        // var created = await PanelProxy.OpenAsync(Panels.Schedule, formVm);
        // if (created is null) return;
        //
        // await State.AddToCollection(created);
    }

}
