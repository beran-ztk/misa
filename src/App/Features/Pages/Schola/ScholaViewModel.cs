using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schola;

public partial class ScholaViewModel(
    ScholaGateway gateway, 
    PanelProxy panelProxy) : ViewModelBase
{
    public async Task InitializeWorkspaceAsync()
    {
        await RefreshWorkspaceAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        // await GetAllAsync();
    }
    
    private async Task GetAllAsync()
    {
        // var values = await _gateway.GetAllAsync();
        // await State.SetMainCollection(values);
    }

    [RelayCommand]
    private async Task ShowAddArcPanelAsync()
    {
        var formVm = new CreateArcViewModel(gateway);

        await panelProxy.OpenAsync(PanelKey.CreateArc, formVm);
    }
    [RelayCommand]
    private async Task ShowAddUnitPanelAsync()
    {
        var formVm = new CreateUnitViewModel(gateway);

        await panelProxy.OpenAsync(PanelKey.CreateUnit, formVm);
    }
}