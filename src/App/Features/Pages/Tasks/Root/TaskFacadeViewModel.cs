using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Features.Entities.Extensions.Items.Extensions.Tasks;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Features.Pages.Tasks.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel(
    TaskState state,
    TaskGateway gateway,
    PanelProxy panelProxy)
    : ViewModelBase
{
    public TaskState State { get; } = state;

    public async Task InitializeWorkspaceAsync()
    {
        await GetAllAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var result = await gateway.GetAllAsync();
        if (result.IsSuccess)
            await State.AddToCollection(result.Value);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateTaskViewModel(State.CreateState, gateway);

        var created = await panelProxy.OpenAsync(Panels.Task, formVm);
        if (created is null) return;

        await State.AddToCollection(created);
    }
}