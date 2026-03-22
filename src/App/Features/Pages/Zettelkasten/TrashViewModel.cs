using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Common.Results;
using Misa.Ui.Avalonia.Common.Mappings;
using Misa.Ui.Avalonia.Infrastructure.UI;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class TrashViewModel(ZettelkastenGateway gateway)
    : ViewModelBase, IHostedForm<Result>
{
    public string  FormTitle       => "Trash";
    public string? FormDescription => null;

    [ObservableProperty] private ObservableCollection<TrashEntryVm> _entries = [];
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private bool _isEmpty;

    public async Task LoadAsync()
    {
        IsLoading = true;
        var deleted = await gateway.GetDeletedKnowledgeAsync();

        Entries.Clear();
        if (deleted is not null)
        {
            var titleById = deleted.ToDictionary(d => d.Id, d => d.Title);
            foreach (var dto in deleted)
            {
                Entries.Add(new TrashEntryVm
                {
                    Id          = dto.Id,
                    Workflow    = dto.Workflow,
                    Title       = dto.Title,
                    ParentId    = dto.ParentId,
                    ParentTitle = dto.ParentId.HasValue && titleById.TryGetValue(dto.ParentId.Value, out var pt) ? pt : null,
                    DeletedAt   = dto.DeletedAt
                });
            }
        }

        IsLoading = false;
        IsEmpty   = Entries.Count == 0;
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RestoreEntry(TrashEntryVm entry)
    {
        // Restore the entry plus any deleted descendants present in the trash list.
        var ids    = CollectWithDescendants(entry).Select(e => e.Id).ToArray();
        var result = await gateway.RestoreSubtreeAsync(ids);
        if (!result.IsSuccess) return;

        foreach (var id in ids)
        {
            var vm = Entries.FirstOrDefault(e => e.Id == id);
            if (vm is not null) Entries.Remove(vm);
        }
        IsEmpty = Entries.Count == 0;
    }

    // ── Permanent delete ──────────────────────────────────────────────────────

    [RelayCommand]
    private async Task HardDeleteEntry(TrashEntryVm entry)
    {
        var result = await gateway.HardDeleteAsync(entry.Id);
        if (!result.IsSuccess) return;

        Entries.Remove(entry);
        IsEmpty = Entries.Count == 0;
    }

    // ── IHostedForm ───────────────────────────────────────────────────────────

    public Task<Result<Result>> SubmitAsync() =>
        Task.FromResult(Result<Result>.Ok(Result.Ok()));

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Yields entry + all entries whose ParentId chain leads back to it.
    private IEnumerable<TrashEntryVm> CollectWithDescendants(TrashEntryVm root)
    {
        yield return root;
        foreach (var child in Entries.Where(e => e.ParentId == root.Id))
            foreach (var desc in CollectWithDescendants(child))
                yield return desc;
    }
}
