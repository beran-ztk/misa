using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Misa.Domain.Items;
using Misa.Ui.Avalonia.Common.Mappings;

namespace Misa.Ui.Avalonia.Features.Pages.Zettelkasten;

public sealed partial class ZettelViewModel : ViewModelBase
{
    private Guid Id { get; set; }

    [ObservableProperty] private string  _title   = string.Empty;
    [ObservableProperty] private string? _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MetadataLine))]
    private DateTimeOffset _createdAt;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MetadataLine))]
    private DateTimeOffset? _modifiedAt;

    [ObservableProperty] private bool _isDirty;

    public string MetadataLine => ModifiedAt.HasValue
        ? $"Created {CreatedAt:MMM d, yyyy}  ·  Modified {ModifiedAt:MMM d, yyyy}"
        : $"Created {CreatedAt:MMM d, yyyy}";

    private CancellationTokenSource? _saveCts;
    private bool _loading;

    // ── Loading ───────────────────────────────────────────────────────────────

    public void Load(Item dto)
    {
        _saveCts?.Cancel();
        _saveCts?.Dispose();
        _saveCts   = null;
        _loading   = true;

        Id         = dto.Id.Value;
        Title      = dto.Title;
        Content    = dto.ZettelExtension.Content;
        CreatedAt  = dto.CreatedAt;
        ModifiedAt = dto.ModifiedAt;
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
        // try
        // {
        //     await Task.Delay(TimeSpan.FromSeconds(2), ct);
        //     var result = await gateway.UpdateZettelContentAsync(Id, content);
        //     if (result.IsSuccess)
        //     {
        //         ModifiedAt = DateTimeOffset.UtcNow;
        //         IsDirty    = false;
        //     }
        // }
        // catch (OperationCanceledException) { }
    }
}
