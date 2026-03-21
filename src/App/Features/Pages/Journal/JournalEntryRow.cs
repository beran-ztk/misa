using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Misa.Contract.Items.Components.Chronicle;

namespace Misa.Ui.Avalonia.Features.Pages.Journal;

/// <summary>
/// Wraps a single journal ChronicleEntryDto with per-row edit and delete state.
/// </summary>
public sealed partial class JournalEntryRow : ObservableObject
{
    private readonly JournalGateway _gateway;
    private readonly Func<Task>     _refresh;

    public JournalEntryRow(ChronicleEntryDto entry, JournalGateway gateway, Func<Task> refresh)
    {
        Entry    = entry;
        _gateway = gateway;
        _refresh = refresh;

        _editContent = entry.Description ?? string.Empty;
        _editTime    = entry.At.ToLocalTime().TimeOfDay;
    }

    public ChronicleEntryDto Entry { get; }

    // ── Edit state ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveEditCommand))]
    private string _editContent;

    [ObservableProperty]
    private TimeSpan? _editTime;

    private bool CanSaveEdit => EditContent.Trim().Length > 0;

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void BeginEdit()
    {
        EditContent = Entry.Description ?? string.Empty;
        EditTime    = Entry.At.ToLocalTime().TimeOfDay;
        IsEditing   = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private async Task SaveEditAsync()
    {
        var localDateTime = DateTime.SpecifyKind(
            Entry.At.ToLocalTime().Date + (EditTime ?? TimeSpan.Zero),
            DateTimeKind.Local);
        var occurredAtUtc = new DateTimeOffset(localDateTime).ToUniversalTime();

        var request = new UpdateJournalRequest(EditContent.Trim(), occurredAtUtc);
        var result  = await _gateway.UpdateAsync(Entry.TargetItemId!.Value, request);

        if (!result.IsSuccess) return;

        IsEditing = false;
        await _refresh();
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (Entry.TargetItemId is null) return;

        var result = await _gateway.DeleteAsync(Entry.TargetItemId.Value);
        if (!result.IsSuccess) return;

        await _refresh();
    }
}
