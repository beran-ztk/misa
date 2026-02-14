using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Scheduling.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerFacadeViewModel : ViewModelBase
{
    public SchedulerState State { get; }
    private SchedulerGateway Gateway { get; }
    private PanelProxy PanelProxy { get; }
    private ModalProxy ModalProxy { get; }

    public SchedulerFacadeViewModel(
        SchedulerState state,
        SchedulerGateway gateway,
        PanelProxy panelProxy,
        ModalProxy modalProxy)
    {
        State = state;
        Gateway = gateway;
        PanelProxy = panelProxy;
        ModalProxy = modalProxy;

        State.CreateState.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(State.CreateState.SelectedActionType))
                _ = ActionTypeChangedAsync();
        };
    }

    private async Task ActionTypeChangedAsync()
    {
        if (State.CreateState.SelectedActionType != ScheduleActionTypeDto.CreateTask)
            return;

        var taskState = new Misa.Ui.Avalonia.Features.Pages.Tasks.Root.CreateTaskState();
        var taskForm = new Misa.Ui.Avalonia.Features.Pages.Tasks.Create.CreateTaskReturnViewModel(taskState);

        var dto = await ModalProxy.OpenAsync<CreateTaskDto>(ModalKey.Task, taskForm);
        if (dto is null)
        {
            State.CreateState.SelectedActionType = ScheduleActionTypeDto.None;
            return;
        }

        State.CreateState.Payload = JsonSerializer.Serialize(dto);
    }

    public async Task InitializeWorkspaceAsync()
        => await GetAllAsync();

    [RelayCommand]
    public async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var items = await Gateway.GetAllAsync();
        await State.AddToCollection(items);
    }

    [RelayCommand]
    public async Task ShowAddPanelAsync()
    {
        State.CreateState.Reset();

        var formVm = new CreateScheduleViewModel(State.CreateState);

        var dto = await PanelProxy.OpenAsync<AddScheduleDto>(PanelKey.Schedule, formVm);
        if (dto is null) return; // Cancel/X

        var item = await Gateway.CreateAsync(dto);
        await State.AddToCollection(item);
    }
}
