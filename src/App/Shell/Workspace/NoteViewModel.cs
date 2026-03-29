using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.App.Infrastructure;
using Misa.Domain;

namespace Misa.App.Shell.Workspace;

public sealed partial class NoteViewModel : ViewModelBase
{
    private Guid Id { get; set; }

    [ObservableProperty] private string  _title   = string.Empty;
    [ObservableProperty] private string? _content;

    [ObservableProperty] private bool _isDirty;

    private CancellationTokenSource? _saveCts;
    private bool _loading;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(Item dto)
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts   = null;
        _loading   = true;

        Id         = dto.Id;
        Title      = dto.Title;
        Content    = dto.Note?.Content;
        IsDirty    = false;

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
            // var result = await gateway.UpdateZettelContentAsync(Id, content);
            if (true)
            {
                IsDirty    = false;
            }
        }
        catch (OperationCanceledException) { }
    }
}