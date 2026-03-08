using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Schola;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Schola;

public partial class ScholaViewModel(
    ScholaGateway gateway, 
    LayerProxy layerProxy) : ViewModelBase
{
    public ObservableCollection<ArcDto> Arcs { get; set; } = [];
    public ObservableCollection<UnitDto> Units { get; set; } = [];
    public async Task InitializeWorkspaceAsync()
    {
        await RefreshWorkspaceAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await GetAllAsync();
    }
    
    private async Task GetAllAsync()
    {
        var response = await gateway.GetAllAsync();
        if (response.IsSuccess && response.Value != null)
        {
            Arcs = new ObservableCollection<ArcDto>(response.Value.Arcs);
            Units = new ObservableCollection<UnitDto>(response.Value.Units);
        }
    }

    [RelayCommand]
    private async Task ShowAddArcPanelAsync()
    {
        var formVm = new CreateArcViewModel(gateway);

        // await panelProxy.OpenAsync(PanelKey.CreateArc, formVm);
    }
    [RelayCommand]
    private async Task ShowAddUnitPanelAsync()
    {
        var formVm = new CreateUnitViewModel(gateway);

        // await panelProxy.OpenAsync(PanelKey.CreateUnit, formVm);
    }
}