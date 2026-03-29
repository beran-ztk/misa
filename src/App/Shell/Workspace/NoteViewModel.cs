using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.App.Infrastructure;
using Misa.Application;
using Misa.Domain;

namespace Misa.App.Shell.Workspace;

public sealed partial class NoteViewModel(Dispatcher dispatcher) : ViewModelBase(dispatcher)
{
    private Guid Id { get; set; }

    [ObservableProperty] private string  _title   = string.Empty;
    [ObservableProperty] private string? _content;

    [ObservableProperty] private bool _isDirty;

    private CancellationTokenSource? _saveCts;
    private bool _loading;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(Item item)
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = null;
        _loading = true;

        Id      = item.Id;
        Title   = item.Title;
        Content = item.Note?.Content;
        IsDirty = false;

        _loading = false;
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

    partial void OnContentChanged(string? value)
    {
        if (_loading) return;
        IsDirty = true;
        ScheduleAutosave(value);
    }

    private void ScheduleAutosave(string? content)
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;
        _ = SaveAfterDelayAsync(content, token);
    }

    private async Task SaveAfterDelayAsync(string? content, CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            // TODO: dispatch UpdateNoteContentCommand once query + update handlers exist
            IsDirty = false;
        }
        catch (OperationCanceledException) { }
    }
}
