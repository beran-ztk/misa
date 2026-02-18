using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Chronicle.Create;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle.Root;

public sealed partial class ChronicleViewModel(
    ChronicleState state,
    ChronicleGateway gateway,
    PanelProxy panelProxy)
    : ViewModelBase
{
    public ChronicleState State { get; } = state;

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
        var result = await gateway.GetAllAsync();
        if (result.IsSuccess)
            await State.AddToCollection(result.Value);
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateJournalViewModel(State.CreateState, gateway);

        var created = await panelProxy.OpenAsync(Panels.Journal, formVm);
        if (created is null) return;

        await State.AddToCollection(created);
    }
}