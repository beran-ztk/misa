using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ImportOverlay : UserControl
{
    private ImportPreview? _preview;
    private CancellationTokenSource? _checkingCancellation;

    public event Action<int>? QueueSubmitted;

    public ImportOverlay()
    {
        InitializeComponent();
        AddInputRow();
    }

    public void Open()
    {
        _preview = null;
        PreviewPanel.IsVisible = false;
        QueueBtn.IsEnabled = false;
        StatusText.Text = string.Empty;
        RefreshQueue();
        IsVisible = true;
        FocusFirstInput();
    }

    public void RefreshQueue()
    {
        QueueSources.Children.Clear();
        var sources = ImportQueueService.Current.GetSources();
        EmptyQueueText.IsVisible = sources.Count == 0;
        foreach (var source in sources)
            QueueSources.Children.Add(CreateSourceCard(source));
    }

    private void AddInputRow(string? url = null)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        var input = new TextBox
        {
            Text = url ?? string.Empty,
            Watermark = "Paste a YouTube video, playlist or mix link…",
            FontSize = 11,
            MinHeight = 34,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        row.Children.Add(input);
        var remove = new Button
        {
            Content = "×", Padding = new Thickness(8, 2), FontSize = 16, Opacity = 0.65,
            IsVisible = InputRows.Children.Count > 0
        };
        ToolTip.SetTip(remove, "Remove link");
        remove.Click += (_, _) =>
        {
            InputRows.Children.Remove(row);
            if (InputRows.Children.Count == 0) AddInputRow();
        };
        Grid.SetColumn(remove, 1);
        row.Children.Add(remove);
        InputRows.Children.Add(row);
    }

    private IEnumerable<string> GetInputUrls() => InputRows.Children.OfType<Grid>()
        .Select(row => row.Children.OfType<TextBox>().FirstOrDefault()?.Text?.Trim())
        .Where(url => !string.IsNullOrWhiteSpace(url))
        .Cast<string>();

    private void FocusFirstInput() => InputRows.Children.OfType<Grid>()
        .Select(row => row.Children.OfType<TextBox>().FirstOrDefault()).FirstOrDefault()?.Focus();

    private void OnAddLinkClicked(object? sender, RoutedEventArgs e)
    {
        AddInputRow();
        FocusFirstInput();
    }

    private async void OnPreviewClicked(object? sender, RoutedEventArgs e)
    {
        var urls = GetInputUrls().ToList();
        if (urls.Count == 0)
        {
            StatusText.Text = "Add at least one YouTube link.";
            return;
        }

        _checkingCancellation?.Cancel();
        _checkingCancellation = new CancellationTokenSource();
        PreviewBtn.IsEnabled = false;
        QueueBtn.IsEnabled = false;
        BusyPanel.IsVisible = true;
        StatusText.Text = string.Empty;
        try
        {
            var progress = new Progress<string>(message => BusyText.Text = message);
            _preview = await ImportQueueService.Current.PreviewAsync(urls, progress, _checkingCancellation.Token);
            ShowPreview(_preview);
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Checking stopped.";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"Could not check links: {exception.Message}";
        }
        finally
        {
            _checkingCancellation?.Dispose();
            _checkingCancellation = null;
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
        foreach (var source in preview.Items.GroupBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase))
            PreviewRows.Children.Add(CreatePreviewSourceCard(source.Key, source));
        PreviewPanel.IsVisible = true;
        QueueBtn.IsEnabled = queued > 0;
        StatusText.Text = queued > 0 ? "Ready to add the new tracks." : "No new tracks to queue.";
    }

    private static Control CreatePreviewSourceCard(string sourceUrl, IEnumerable<ImportPreviewItem> items)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#151B22")), BorderBrush = new SolidColorBrush(Color.Parse("#2D3D4B")),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(9, 7)
        };
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = ShortUrl(sourceUrl), FontSize = 10, Foreground = new SolidColorBrush(Color.Parse("#9FCBE4")), TextTrimming = TextTrimming.CharacterEllipsis });
        foreach (var item in items)
            panel.Children.Add(CreateItemRow(item.Title, item.Status,
                item.Status == ImportQueueStatus.Queued
                    ? item.EstimatedSizeBytes is long size ? FormatBytes(size) : "new"
                    : item.Detail ?? "unavailable"));
        card.Child = panel;
        return card;
    }

    private Control CreateSourceCard(ImportQueueSource source)
    {
        var card = new Border { Background = new SolidColorBrush(Color.Parse("#151B22")), BorderBrush = new SolidColorBrush(Color.Parse("#2D3D4B")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 8) };
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(new TextBlock { Text = ShortUrl(source.SourceUrl), FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#9FCBE4")), TextTrimming = TextTrimming.CharacterEllipsis });
        foreach (var item in source.Items)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
            row.Children.Add(CreateItemRow(item.Title, item.Status, item.Detail ?? StatusLabel(item.Status)));
            if (item.Status == ImportQueueStatus.Queued)
            {
                var remove = new Button { Content = "Remove", FontSize = 9, Padding = new Thickness(6, 2), Opacity = 0.7 };
                remove.Click += (_, _) =>
                {
                    if (ImportQueueService.Current.RemoveQueuedItem(item.Id)) RefreshQueue();
                };
                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);
            }
            panel.Children.Add(row);
        }
        card.Child = panel;
        return card;
    }

    private static Control CreateItemRow(string title, ImportQueueStatus status, string detail)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(0, 1) };
        row.Children.Add(new TextBlock { Text = title, FontSize = 10.5, Foreground = new SolidColorBrush(Color.Parse(status is ImportQueueStatus.Queued or ImportQueueStatus.ReadyForReview ? "#DDE8F0" : "#8D9AA7")), TextTrimming = TextTrimming.CharacterEllipsis });
        var state = new TextBlock { Text = detail, FontSize = 9.5, Foreground = new SolidColorBrush(Color.Parse(StatusColor(status))), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        Grid.SetColumn(state, 1);
        row.Children.Add(state);
        return row;
    }

    private void OnQueueClicked(object? sender, RoutedEventArgs e)
    {
        if (_preview is null) return;
        var count = _preview.Items.Count(item => item.Status == ImportQueueStatus.Queued);
        ImportQueueService.Current.Queue(_preview);
        QueueSubmitted?.Invoke(count);
        _preview = null;
        PreviewPanel.IsVisible = false;
        QueueBtn.IsEnabled = false;
        StatusText.Text = string.Empty;
        RefreshQueue();
    }

    private void OnStopCheckingClicked(object? sender, RoutedEventArgs e) => _checkingCancellation?.Cancel();
    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _checkingCancellation?.Cancel();
        IsVisible = false;
    }

    private static string StatusLabel(ImportQueueStatus status) => status switch
    {
        ImportQueueStatus.Queued => "waiting",
        ImportQueueStatus.Downloading => "downloading",
        ImportQueueStatus.Analyzing => "analyzing",
        ImportQueueStatus.ReadyForReview => "ready for review",
        ImportQueueStatus.Failed => "failed",
        _ => "skipped"
    };

    private static string StatusColor(ImportQueueStatus status) => status switch
    {
        ImportQueueStatus.Downloading or ImportQueueStatus.Analyzing => "#5BBEED",
        ImportQueueStatus.ReadyForReview => "#E6BF55",
        ImportQueueStatus.Failed => "#E87878",
        ImportQueueStatus.Queued => "#9FCBE4",
        _ => "#8996A3"
    };

    private static string ShortUrl(string url) => url.Length > 78 ? url[..75] + "…" : url;
    private static string FormatBytes(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1_000_000_000d:0.0} GB" : $"{bytes / 1_000_000d:0} MB";
    private static string FormatDuration(TimeSpan value) => value.TotalMinutes >= 1 ? $"{Math.Ceiling(value.TotalMinutes):0} min" : $"{Math.Max(1, Math.Round(value.TotalSeconds)):0} sec";
}
