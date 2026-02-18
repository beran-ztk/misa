using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerFacadeViewModel(
    SchedulerState state,
    SchedulerGateway gateway,
    PanelProxy panelProxy)
    : ViewModelBase
{
    public SchedulerState State { get; } = state;
    private SchedulerGateway Gateway { get; } = gateway;
    private PanelProxy PanelProxy { get; } = panelProxy;

    public async Task InitializeWorkspaceAsync()
        => await GetAllAsync();

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var result = await Gateway.GetAllAsync();
        
        if (result.IsSuccess)
            await State.AddToCollection(result.Value ?? []);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        State.CreateState.Reset();

        var formVm = new CreateScheduleViewModel(State.CreateState, Gateway);

        var created = await PanelProxy.OpenAsync(Panels.Schedule, formVm);
        if (created is null) return;

        await State.AddToCollection(created);
    }

}
