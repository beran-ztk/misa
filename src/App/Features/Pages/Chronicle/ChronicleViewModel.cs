using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData.Binding;
using Misa.Contract.Items;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Chronicle;

public record ChronicleEntry(string Title, DateTime OccurredAt);

public sealed partial class ChronicleItems(DateTime date, List<ChronicleEntry> entries) : ObservableObject
{
    public DateTime Date { get; set; } = date;
    public ObservableCollection<ChronicleEntry> Entries { get; set; } = new(entries);
}
public partial class ChronicleViewModel(ChronicleGateway gateway, PanelProxy panelProxy) : ViewModelBase
{
    public IReadOnlyCollection<ItemDto> Entries { get; set; } = [];
    public ObservableCollection<ChronicleItems> ChronicleEntries { get; set; } = [];

    public void TransformItemsToChronicleEntries()
    {
        var groups = Entries
            .Where(e => e.JournalExtension?.OccurredAt != null)
            .Select(c => new ChronicleEntry(c.Title, c.JournalExtension!.OccurredAt.DateTime))
            .GroupBy(e => e.OccurredAt.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new ChronicleItems(g.Key, g.OrderByDescending(c => c.OccurredAt).ToList()))
            .ToList();

        ChronicleEntries = new ObservableCollectionExtended<ChronicleItems>(groups);
        OnPropertyChanged(nameof(ChronicleEntries));
    }
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
        Entries = await gateway.GetAllAsync();

        TransformItemsToChronicleEntries();
    }

    [RelayCommand]
    private async Task ShowAddPanelAsync()
    {
        var formVm = new CreateJournalViewModel(gateway);
        
        var created = await panelProxy.OpenAsync(PanelKey.CreateJournal, formVm);
        if (created.IsSuccess)
        {
            await GetAllAsync();
        }
    }
}