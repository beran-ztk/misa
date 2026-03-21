using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    private Guid Id { get; set; }

    [ObservableProperty] private string  _title   = string.Empty;
    [ObservableProperty] private string? _content;

    public IReadOnlyList<int> LineNumbers { get; private set; } = [1];

    private CancellationTokenSource? _saveCts;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(ZettelDto dto)
    {
        Id      = dto.Id;
        Title   = dto.Title;
        Content = dto.Content;
        RebuildLineNumbers(dto.Content);
    }

    // ── Line numbers ──────────────────────────────────────────────────────────

    partial void OnContentChanged(string? value)
    {
        RebuildLineNumbers(value);
        ScheduleAutosave(value);
    }

    private void RebuildLineNumbers(string? content)
    {
        var count = string.IsNullOrEmpty(content) ? 1 : content.Count(c => c == '\n') + 1;
        LineNumbers = Enumerable.Range(1, count).ToArray();
        OnPropertyChanged(nameof(LineNumbers));
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

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
            await gateway.UpdateZettelContentAsync(Id, content);
        }
        catch (OperationCanceledException) { }
    }
}
