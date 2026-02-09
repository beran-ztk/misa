using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;

namespace Misa.Ui.Avalonia.Features.Pages.Scheduling.Root;

public sealed partial class SchedulerFacadeViewModel(
    SchedulerState state,
    SchedulerGateway gateway,
    PanelProxy panelProxy)
    : ViewModelBase, IItemFacade
{
    public SchedulerState State { get; } = state;

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
        var items = await gateway.GetAllAsync();
        await State.AddToCollection(items);
    }

    [RelayCommand]
    public void ShowAddPanel()
    {
        State.CreateState.Reset();
        panelProxy.OpenAddSchedule(this);
    }

    [RelayCommand]
    public void ClosePanel()
        => panelProxy.Close();

    [RelayCommand]
    public async Task SubmitCreateAsync()
    {
        var dto = State.CreateState.TryGetValidatedRequestObject();
        if (dto is null) return;

        var item = await gateway.CreateAsync(dto);
        await State.AddToCollection(item);

        ClosePanel();
    }
}