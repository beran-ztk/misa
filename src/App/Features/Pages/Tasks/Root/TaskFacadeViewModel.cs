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
        => await GetAllAsync();

    [RelayCommand]
    public async Task RefreshWorkspaceAsync()
    {
        State.SelectedItem = null;
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var tasks = await gateway.GetAllAsync();
        await State.AddToCollection(tasks);
    }

    [RelayCommand]
    public async Task ShowAddPanelAsync()
    {
        var formVm = new CreateTaskViewModel(State.CreateState);

        var dto = await panelProxy.OpenAsync<CreateTaskDto>(PanelKey.Task, formVm);
        if (dto is null) return;

        var created = await gateway.CreateAsync(dto);
        await State.AddToCollection(created);
    }
}