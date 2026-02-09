using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Features.Pages.Common;
using Misa.Ui.Avalonia.Infrastructure.Client;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Tasks.Root;

public sealed partial class TaskFacadeViewModel(
    TaskState state, 
    TaskGateway gateway,
    PanelProxy panelProxy) 
    : ViewModelBase, IItemFacade
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
    public void ShowAddPanel()
        => panelProxy.Open(PanelKey.Task, this);

    [RelayCommand]
    public void ClosePanel()
        => panelProxy.Close();

    [RelayCommand]
    public async Task SubmitCreateAsync()
    {
        var dto = State.CreateState.TryGetValidatedRequestObject();
        if (dto is null) return;

        var task = await gateway.CreateAsync(dto);
        await State.AddToCollection(task);

        ClosePanel();
    }
}