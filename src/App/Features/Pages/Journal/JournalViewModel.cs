using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

public sealed partial class JournalViewModel(JournalGateway gateway, LayerProxy layerProxy) : ViewModelBase
{
    public async Task InitializeWorkspaceAsync() => await LoadAsync();

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await LoadAsync();
    }
    private async Task LoadAsync()
    {
        
    }
    
    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateJournalViewModel(gateway);
        var result = await layerProxy.OpenAsync<CreateJournalViewModel, Result>(formVm);
        if (result is { IsSuccess: true })
            await LoadAsync();
    }
}