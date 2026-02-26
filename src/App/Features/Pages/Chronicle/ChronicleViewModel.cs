using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public partial class ChronicleViewModel(ChronicleGateway gateway, PanelProxy panelProxy) : ViewModelBase
{
    public async Task InitializeWorkspaceAsync()
    {
        await GetAllAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await GetAllAsync();
    }

    private async Task GetAllAsync()
    {
        var values = await gateway.GetAllAsync();
        // await State.SetMainCollection(values);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        // var formVm = new CreateTaskViewModel(State.CreateState, _gateway);
        //
        // var created = await _panelProxy.OpenAsync(Panels.Task, formVm);
        // if (created is null) return;
        //
        // await State.AppendToMainCollection(created);
    }
}