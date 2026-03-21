using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelkastenViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    public ObservableCollection<KnowledgeIndexEntryDto> KnowledgeIndex { get; } = [];
    
    // ── Initialization ────────────────────────────────────────────────────────

    public async Task InitializeWorkspaceAsync()
    {
        await LoadIndexAsync();
    }

    [RelayCommand]
    private async Task RefreshWorkspaceAsync()
    {
        await LoadIndexAsync();
    }
    
    // ── Tree reload helpers ───────────────────────────────────────────────────
    private async Task LoadIndexAsync()
    {
        var indexEntry = await gateway.GetKnowledgeIndexAsync();

        if (indexEntry is not null)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                KnowledgeIndex.Clear();
                
                foreach (var entry in indexEntry)
                {
                    KnowledgeIndex.Add(entry);
                }
            });
        }
    }
}
