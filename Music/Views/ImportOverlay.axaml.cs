using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ImportOverlay : UserControl
{
    private ImportPreview? _preview;
    private bool _isChecking;

    public event Action<int>? QueueSubmitted;

    public ImportOverlay() => InitializeComponent();

    public void Open()
    {
        UrlsBox.Text = string.Empty;
        StatusText.Text = string.Empty;
        PreviewPanel.IsVisible = false;
        PreviewRows.Children.Clear();
        QueueBtn.IsEnabled = false;
        _preview = null;
        _isChecking = false;
        IsVisible = true;
        UrlsBox.Focus();
    }

    private async void OnPreviewClicked(object? sender, RoutedEventArgs e)
    {
        var urls = (UrlsBox.Text ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (urls.Count == 0)
        {
            StatusText.Text = "Paste at least one YouTube link.";
            return;
        }

        _isChecking = true;
        PreviewBtn.IsEnabled = false;
        QueueBtn.IsEnabled = false;
        BusyPanel.IsVisible = true;
        BusyText.Text = "Reading playlists and checking your library…";
        StatusText.Text = string.Empty;
        try
        {
            _preview = await ImportQueueService.Current.PreviewAsync(urls);
            ShowPreview(_preview);
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not check links: {exception.Message}";
        }
        finally
        {
            _isChecking = false;
            PreviewBtn.IsEnabled = true;
            BusyPanel.IsVisible = false;
        }
    }

    private void ShowPreview(ImportPreview preview)
    {
        PreviewRows.Children.Clear();
        var queued = preview.Items.Count(item => item.Status == ImportQueueStatus.Queued);
        PreviewSummaryText.Text = $"{preview.Items.Count} found · {queued} new · {preview.ExistingCount} already in library"
            + (preview.DuplicateCount > 0 ? $" · {preview.DuplicateCount} duplicates" : string.Empty)
            + (preview.UnavailableCount > 0 ? $" · {preview.UnavailableCount} unavailable" : string.Empty)
            + (preview.TotalEstimatedSizeBytes is long size ? $"\nEstimated size: {FormatBytes(size)}" : string.Empty)
            + (preview.EstimatedDownloadTime is TimeSpan download ? $" · Download: {FormatDuration(download)}" : string.Empty)
            + (preview.EstimatedAnalysisTime is TimeSpan analysis ? $" · Analysis: {FormatDuration(analysis)}" : string.Empty);

        foreach (var item in preview.Items)
            PreviewRows.Children.Add(CreatePreviewRow(item));
        PreviewPanel.IsVisible = true;
        QueueBtn.IsEnabled = queued > 0;
        StatusText.Text = queued > 0 ? "Review the plan, then add the new tracks to the background queue." : "No new tracks to queue.";
    }

    private static Control CreatePreviewRow(ImportPreviewItem item)
    {
        var queued = item.Status == ImportQueueStatus.Queued;
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Avalonia.Thickness(0, 0, 0, 2) };
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse(queued ? "#DDE8F0" : "#8D9AA7")),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        row.Children.Add(title);
        var state = item.Status switch
        {
            ImportQueueStatus.Queued => item.EstimatedSizeBytes is long size ? FormatBytes(size) : "new",
            ImportQueueStatus.Skipped => item.Detail ?? "skipped",
            _ => item.Detail ?? "unavailable"
        };
        var stateText = new TextBlock
        {
            Text = state,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse(queued ? "#79C4E8" : "#9AA5AF")),
            Margin = new Avalonia.Thickness(12, 0, 0, 0)
        };
        Grid.SetColumn(stateText, 1);
        row.Children.Add(stateText);
        return row;
    }

    private void OnQueueClicked(object? sender, RoutedEventArgs e)
    {
        if (_preview is null) return;
        var count = _preview.Items.Count(item => item.Status == ImportQueueStatus.Queued);
        ImportQueueService.Current.Queue(_preview);
        QueueSubmitted?.Invoke(count);
        IsVisible = false;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (!_isChecking) IsVisible = false;
    }

    private static string FormatBytes(long bytes) => bytes >= 1_000_000_000
        ? $"{bytes / 1_000_000_000d:0.0} GB"
        : $"{bytes / 1_000_000d:0} MB";

    private static string FormatDuration(TimeSpan value) => value.TotalMinutes >= 1
        ? $"{Math.Ceiling(value.TotalMinutes):0} min"
        : $"{Math.Max(1, Math.Round(value.TotalSeconds)):0} sec";

    private void OnBackdropPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (!_isChecking) IsVisible = false;
    }
}
