using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerFacadeViewModel(
    SchedulerState state, 
    SchedulerGateway gateway,
    PanelProxy panelProxy) 
    : ViewModelBase
{
    public SchedulerState State { get; } = state;
    private SchedulerGateway Gateway { get; } = gateway;
    
    public async Task InitializeWorkspace()
    {
        await GetAllTasksAsync();
    }

    [RelayCommand]
    private void RefreshTaskWindow()
    {
        _ = GetAllTasksAsync();
        State.SelectedItem = null;
    }

    private async Task GetAllTasksAsync()
    {
        var items = await Gateway.GetAllAsync();
        
        await State.AddToCollection(items);
    }
    // Create Schedule
    [RelayCommand]
    private void ShowAddPanel()
    {
        State.CreateScheduleState.Reset();
        panelProxy.OpenAddSchedule(this);
    }
    [RelayCommand]
    private void CloseAddPanel() => panelProxy.Close();
    [RelayCommand]
    private async Task SubmitCreateSchedule()
    {
        var dto = State.CreateScheduleState.TryGetValidatedRequestObject();
        if (dto is null) return;

        var item = await Gateway.CreateAsync(dto);
        await State.AddToCollection(item);
        CloseAddPanel();
    }
}