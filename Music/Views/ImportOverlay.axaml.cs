using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ImportOverlay : UserControl
{
    private readonly List<PendingImportPreview> _pendingPreviews = [];
    private readonly List<CancellationTokenSource> _checkingTokens = [];
    private readonly DispatcherTimer _inputDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };

    public event Action<int>? QueueSubmitted;

    public ImportOverlay()
    {
        InitializeComponent();
        _inputDebounceTimer.Tick += (_, _) =>
        {
            _inputDebounceTimer.Stop();
            StartCheckingCurrentInput();
        };
        InputUrlBox.TextChanged += (_, _) =>
        {
            StatusText.Text = string.Empty;
            _inputDebounceTimer.Stop();
            if (!string.IsNullOrWhiteSpace(InputUrlBox.Text))
                _inputDebounceTimer.Start();
        };
    }

    public void Open()
    {
        StatusText.Text = string.Empty;
        RebuildPendingPreviews();
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

    private void FocusFirstInput() => InputUrlBox.Focus();

    private async void StartCheckingCurrentInput()
    {
        var url = InputUrlBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
            return;

        InputUrlBox.Text = string.Empty;
        StatusText.Text = string.Empty;
        var pending = new PendingImportPreview(url);
        _pendingPreviews.Insert(0, pending);
        RebuildPendingPreviews();

        var cancellation = new CancellationTokenSource();
        _checkingTokens.Add(cancellation);
        try
        {
            var progress = new Progress<string>(message =>
            {
                pending.StatusText = message;
                RebuildPendingPreviews();
            });
            pending.Preview = await ImportQueueService.Current.PreviewAsync([url], progress, cancellation.Token);
            pending.StatusText = PreviewStatus(pending.Preview);
        }
        catch (OperationCanceledException)
        {
            pending.StatusText = "Checking stopped.";
        }
        catch (Exception exception)
        {
            pending.StatusText = $"Could not check link: {exception.Message}";
        }
        finally
        {
            pending.IsChecking = false;
            _checkingTokens.Remove(cancellation);
            cancellation.Dispose();
            RebuildPendingPreviews();
        }
    }

    private void RebuildPendingPreviews()
    {
        PendingPreviewRows.Children.Clear();
        EmptyPreviewText.IsVisible = _pendingPreviews.Count == 0;
        foreach (var pending in _pendingPreviews)
            PendingPreviewRows.Children.Add(CreatePendingPreviewCard(pending));
    }

    private static string PreviewStatus(ImportPreview preview)
    {
        var queued = preview.Items.Count(item => item.Status == ImportQueueStatus.Queued);
        return $"{preview.Items.Count} found · {queued} new · {preview.ExistingCount} already in library"
            + (preview.DuplicateCount > 0 ? $" · {preview.DuplicateCount} duplicates" : string.Empty)
            + (preview.UnavailableCount > 0 ? $" · {preview.UnavailableCount} unavailable" : string.Empty)
            + (preview.TotalEstimatedSizeBytes is long size ? $"\nEstimated size: {FormatBytes(size)}" : string.Empty)
            + (preview.EstimatedDownloadTime is TimeSpan download ? $" · Download: {FormatDuration(download)}" : string.Empty)
            + (preview.EstimatedAnalysisTime is TimeSpan analysis ? $" · Analysis: {FormatDuration(analysis)}" : string.Empty);
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

    private Control CreatePendingPreviewCard(PendingImportPreview pending)
    {
        var hasQueuedItems = pending.Preview?.Items.Any(item => item.Status == ImportQueueStatus.Queued) == true;
        var borderColor = pending.Preview is null
            ? pending.IsChecking ? "#2D3D4B" : "#7A3434"
            : hasQueuedItems ? "#2E6D47" : "#5A6470";
        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#151B22")),
            BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 8)
        };
        var panel = new StackPanel { Spacing = 7 };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 7 };
        header.Children.Add(new TextBlock
        {
            Text = ShortUrl(pending.SourceUrl),
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse("#9FCBE4")),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        if (pending.Preview is { } preview && preview.Items.Any(item => item.Status == ImportQueueStatus.Queued))
        {
            var queue = new Button { Content = "Queue →", FontSize = 10, Padding = new Thickness(8, 3) };
            queue.Click += (_, _) =>
            {
                var count = preview.Items.Count(item => item.Status == ImportQueueStatus.Queued);
                ImportQueueService.Current.Queue(preview);
                QueueSubmitted?.Invoke(count);
                _pendingPreviews.Remove(pending);
                RebuildPendingPreviews();
                RefreshQueue();
            };
            Grid.SetColumn(queue, 1);
            header.Children.Add(queue);
        }
        var remove = new Button
        {
            Content = "×",
            FontSize = 14,
            Padding = new Thickness(7, 1),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Opacity = 0.72
        };
        remove.Click += (_, _) =>
        {
            _pendingPreviews.Remove(pending);
            RebuildPendingPreviews();
        };
        Grid.SetColumn(remove, 2);
        header.Children.Add(remove);
        panel.Children.Add(header);

        if (pending.Preview is null)
        {
            panel.Children.Add(new TextBlock
            {
                Text = pending.StatusText,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse(pending.IsChecking ? "#DDE8F0" : "#E87878")),
                Opacity = 0.68,
                TextWrapping = TextWrapping.Wrap
            });
            if (pending.IsChecking)
            {
                var progress = new ProgressBar
                {
                    IsIndeterminate = true,
                    Height = 4,
                    Foreground = new SolidColorBrush(Color.Parse("#1E9AF0")),
                    Background = new SolidColorBrush(Color.Parse("#26313A"))
                };
                panel.Children.Add(progress);
            }
        }
        else
        {
            panel.Children.Add(new TextBlock
            {
                Text = pending.StatusText,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#AEE6B7")),
                TextWrapping = TextWrapping.Wrap
            });
            foreach (var source in pending.Preview.Items.GroupBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase))
                panel.Children.Add(CreatePreviewSourceCard(source.Key, source));
        }

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
            if (item.Status is ImportQueueStatus.Queued or ImportQueueStatus.Failed)
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

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _inputDebounceTimer.Stop();
        foreach (var token in _checkingTokens.ToList())
            token.Cancel();
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

    private sealed class PendingImportPreview
    {
        public PendingImportPreview(string sourceUrl)
        {
            SourceUrl = sourceUrl;
        }

        public string SourceUrl { get; }
        public ImportPreview? Preview { get; set; }
        public bool IsChecking { get; set; } = true;
        public string StatusText { get; set; } = "Reading link…";
    }
}
