using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelViewModel(ZettelkastenGateway gateway) : ViewModelBase
{
    public Guid Id { get; private set; }

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string? _content;

    private CancellationTokenSource? _saveCts;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(ZettelDto dto)
    {
        Id       = dto.Id;
        _title   = dto.Title;
        _content = dto.Content;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Content));
    }

    // ── Autosave ──────────────────────────────────────────────────────────────

    partial void OnContentChanged(string? value)
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;
        _ = SaveAfterDelayAsync(value, token);
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
