using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public partial class ChronicleViewModel(ChronicleGateway gateway, PanelProxy panelProxy) : ViewModelBase
{
    public ObservableCollection<ItemDto> Entries { get; } = [];
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
        var entries = await gateway.GetAllAsync();
        Entries.Clear();
        
        foreach (var entry in entries)
        {
            await Dispatcher.UIThread.InvokeAsync(() => 
            {
                Entries.Add(entry);
            });
        }
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateJournalViewModel(gateway);
        
        var created = await panelProxy.OpenAsync(PanelKey.CreateJournal, formVm);
        if (created.IsSuccess)
            await GetAllAsync();
    }
}