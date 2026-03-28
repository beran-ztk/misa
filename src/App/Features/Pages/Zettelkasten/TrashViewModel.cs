using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class TrashViewModel : ViewModelBase, IHostedForm<object>
{
    public string  FormTitle       => "Trash";
    public string? FormDescription => null;

    [ObservableProperty] private ObservableCollection<KnowledgeIndexNodeVm> _entries = [];
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEmpty;

    public async Task LoadAsync()
    {
        IsLoading = true;
        //
        // var deleted = await gateway.GetDeletedKnowledgeAsync();
        //
        // Entries.Clear();
        // if (deleted is not null)
        // {
        //     foreach (var root in KnowledgeTreeBuilder.FromDeletedFlat(deleted))
        //         Entries.Add(root);
        // }
        //
        // IsLoading = false;
        // IsEmpty   = Entries.Count == 0;
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RestoreEntry(KnowledgeIndexNodeVm entry)
    {
        // var ids    = entry.GetSubtreeIds().ToArray();
        // var result = await gateway.RestoreSubtreeAsync(ids);
        // if (!result.IsSuccess) return;
        //
        // await LoadAsync(); // rebuild the tree; the restored items will no longer appear
    }

    // ── Permanent delete ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task HardDeleteEntry(KnowledgeIndexNodeVm entry)
    {
        // var result = await gateway.HardDeleteAsync(entry.Id);
        // if (!result.IsSuccess) return;
        //
        // await LoadAsync(); // rebuild; the deleted item (and its children) will no longer appear
    }

    // ── IHostedForm ───────────────────────────────────────────────────────────

    public Task<object> SubmitAsync()
    {
        return new object();
    }
}
