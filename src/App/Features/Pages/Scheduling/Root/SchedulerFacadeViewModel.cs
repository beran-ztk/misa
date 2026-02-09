using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerFacadeViewModel : ViewModelBase, IItemFacade
{
    public SchedulerState State { get; } 
    private SchedulerGateway Gateway { get; }
    private PanelProxy PanelProxy { get; }

    public SchedulerFacadeViewModel(SchedulerState state, SchedulerGateway gateway, PanelProxy panelProxy)
    {
        State = state;
        Gateway = gateway;
        PanelProxy = panelProxy;

        State.CreateState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(State.CreateState.SelectedActionType))
                ActionTypeChanged();
        };
    }
    private void ActionTypeChanged()
    {
        switch (State.CreateState.SelectedActionType)
        {
            case ScheduleActionTypeDto.CreateTask:
                break;
        }
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
    public void ShowAddPanel()
    {
        State.CreateState.Reset();
        PanelProxy.Open(PanelKey.Schedule, this);
    }

    [RelayCommand]
    public void ClosePanel()
        => PanelProxy.Close();

    [RelayCommand]
    public async Task SubmitCreateAsync()
    {
        var dto = State.CreateState.TryGetValidatedRequestObject();
        if (dto is null) return;

        var item = await Gateway.CreateAsync(dto);
        await State.AddToCollection(item);

        ClosePanel();
    }
}