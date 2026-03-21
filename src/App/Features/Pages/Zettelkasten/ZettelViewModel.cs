using System;
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

    private CancellationTokenSource? _saveCts;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(ZettelDto dto)
    {
        Id      = dto.Id;
        Title   = dto.Title;
        Content = dto.Content;
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

    partial void OnContentChanged(string? value) => ScheduleAutosave(value);

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
