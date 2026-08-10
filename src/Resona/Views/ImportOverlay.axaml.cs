using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Resona.Models;
using Resona.Services;

namespace Resona.Views;

public partial class ImportOverlay : UserControl
{
    private readonly List<PendingImportPreview> _pendingPreviews = [];
    private readonly List<CancellationTokenSource> _checkingTokens = [];
    private readonly DispatcherTimer _inputDebounceTimer = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private readonly DispatcherTimer _queueElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public event Action<int>? QueueSubmitted;
    public event Action<string>? ToastRequested;

    public ImportOverlay()
    {
        InitializeComponent();
        _inputDebounceTimer.Tick += (_, _) =>
        {
            _inputDebounceTimer.Stop();
            StartCheckingCurrentInput();
        };
        _queueElapsedTimer.Tick += (_, _) =>
        {
            if (IsVisible)
                RefreshQueue();
            else
                _queueElapsedTimer.Stop();
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
        UpdateQueueElapsedTimer();
        FocusFirstInput();
    }

    public void RefreshQueue()
    {
        QueueSources.Children.Clear();
        var sources = ImportQueueService.Current.GetSources();
        var queueCount = sources.Sum(source => source.Items.Count);
        QueueHeaderText.Text = queueCount > 0 ? $"CURRENT QUEUE ({queueCount})" : "CURRENT QUEUE";
        EmptyQueueText.IsVisible = sources.Count == 0;
        foreach (var source in sources)
            QueueSources.Children.Add(CreateSourceCard(source));

        AnalysisQueueRows.Children.Clear();
        var analysisItems = GetAnalysisQueueItems();
        var analysisSnapshot = BackgroundAnalysisService.Current.GetSnapshot();
        AnalysisQueueHeaderText.Text = analysisItems.Count > 0 ? $"ANALYSIS QUEUE ({analysisItems.Count})" : "ANALYSIS QUEUE";
        var isCheckingServer = analysisSnapshot.ServerConnectionState == AnalysisServerConnectionState.Checking;
        AnalysisServerUnavailablePanel.IsVisible = analysisSnapshot.ServerConnectionState is
            AnalysisServerConnectionState.Checking or AnalysisServerConnectionState.Unreachable;
        AnalysisServerUnavailableText.Text = isCheckingServer
            ? "Testing server connection…"
            : "Server currently unavailable.";
        RetryAnalysisServerButton.IsEnabled = !isCheckingServer;
        EmptyAnalysisQueueText.IsVisible = analysisItems.Count == 0;
        foreach (var item in analysisItems)
            AnalysisQueueRows.Children.Add(CreateAnalysisQueueRow(item));

        UpdateQueueElapsedTimer(sources, analysisItems);
    }

    private async void OnRetryAnalysisServerClicked(object? sender, RoutedEventArgs e)
    {
        RetryAnalysisServerButton.IsEnabled = false;
        var isReachable = await BackgroundAnalysisService.Current.RetryServerConnectionAsync();
        RefreshQueue();
        ToastRequested?.Invoke(isReachable
            ? "Analysis server connected."
            : "Analysis server is still unavailable.");
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

    private static string PreviewStatus(ImportPreview preview, IReadOnlyList<ImportPreviewItem>? visibleItems = null,
        int? availableCount = null)
    {
        var items = visibleItems ?? preview.Items;
        var total = availableCount ?? preview.Items.Count;
        var queued = items.Count(item => item.Status == ImportQueueStatus.Queued);
        var existing = visibleItems is null
            ? preview.ExistingCount
            : items.Count(item => item.Status == ImportQueueStatus.Skipped
                                  && item.Detail?.Equals("Already in library", StringComparison.OrdinalIgnoreCase) == true);
        var duplicates = visibleItems is null
            ? preview.DuplicateCount
            : items.Count(item => item.Status == ImportQueueStatus.Skipped
                                  && item.Detail?.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true);
        var unavailable = visibleItems is null
            ? preview.UnavailableCount
            : items.Count(item => item.Status == ImportQueueStatus.Failed);
        var found = visibleItems is null || items.Count == total
            ? $"{total} found"
            : $"{items.Count} of {total} shown";
        var includeEstimates = visibleItems is null || items.Count == preview.Items.Count;

        return $"{found} · {queued} new · {existing} already in library"
            + (duplicates > 0 ? $" · {duplicates} duplicates" : string.Empty)
            + (unavailable > 0 ? $" · {unavailable} unavailable" : string.Empty)
            + (includeEstimates && preview.TotalEstimatedSizeBytes is long size ? $"\nEstimated size: {FormatBytes(size)}" : string.Empty)
            + (includeEstimates && preview.EstimatedDownloadTime is TimeSpan download ? $" · Download: {FormatDuration(download)}" : string.Empty)
            + (includeEstimates && preview.EstimatedAnalysisTime is TimeSpan analysis ? $" · Analysis: {FormatDuration(analysis)}" : string.Empty);
    }

    private Control CreatePreviewSourceCard(PendingImportPreview pending, string sourceUrl,
        IEnumerable<ImportPreviewItem> items, Action refreshPreview)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#C8151B22")), BorderBrush = new SolidColorBrush(Color.Parse("#2D3D4B")),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(9, 7)
        };
        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(new TextBlock { Text = ShortUrl(sourceUrl), FontSize = 10, Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"), TextTrimming = TextTrimming.CharacterEllipsis });
        foreach (var item in items)
            panel.Children.Add(CreatePreviewItemRow(pending, item, refreshPreview));
        card.Child = panel;
        return card;
    }

    private static Control CreatePreviewItemRow(PendingImportPreview pending, ImportPreviewItem item, Action refreshPreview)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"), ColumnSpacing = 7, Margin = new Thickness(0, 1) };
        row.Children.Add(new TextBlock
        {
            Text = item.DurationSeconds is int seconds ? FormatTrackDuration(seconds) : "--:--",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#798796")),
            MinWidth = 34,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse(item.Status is ImportQueueStatus.Queued or ImportQueueStatus.ReadyForReview ? "#DDE8F0" : "#8D9AA7")),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(title, 1);
        row.Children.Add(title);
        var detail = item.Status == ImportQueueStatus.Queued
            ? item.EstimatedSizeBytes is long size ? FormatBytes(size) : "new"
            : item.Detail ?? "unavailable";
        var state = new TextBlock
        {
            Text = detail,
            FontSize = 9.5,
            Foreground = new SolidColorBrush(Color.Parse(StatusColor(item.Status))),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(state, 2);
        row.Children.Add(state);
        var remove = new Button
        {
            Content = "×",
            FontSize = 12,
            Padding = new Thickness(5, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Opacity = 0.62
        };
        remove.Click += (_, _) =>
        {
            pending.RemovedCanonicalUrls.Add(item.CanonicalUrl);
            refreshPreview();
        };
        Grid.SetColumn(remove, 3);
        row.Children.Add(remove);
        return row;
    }

    private Control CreatePendingPreviewCard(PendingImportPreview pending)
    {
        var hasQueuedItems = pending.Preview?.Items.Any(item => item.Status == ImportQueueStatus.Queued) == true;
        var borderColor = pending.Preview is null
            ? pending.IsChecking ? "#2D3D4B" : "#7A3434"
            : hasQueuedItems ? "#2E6D47" : "#5A6470";
        var card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#C8151B22")),
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
            Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        Button? queue = null;
        if (pending.Preview is { } preview)
        {
            queue = new Button { Content = "Queue →", FontSize = 10, Padding = new Thickness(8, 3) };
            queue.Click += (_, _) =>
            {
                var visibleItems = GetVisiblePreviewItems(pending);
                var count = visibleItems.Count(item => item.Status == ImportQueueStatus.Queued);
                if (count == 0) return;

                ImportQueueService.Current.Queue(preview with { Items = visibleItems });
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
                    Foreground = ThemeResources.Brush("Theme.Brush.Accent"),
                    Background = new SolidColorBrush(Color.Parse("#3B392F"))
                };
                panel.Children.Add(progress);
            }
        }
        else
        {
            var status = new TextBlock
            {
                Text = pending.StatusText,
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#AEE6B7")),
                TextWrapping = TextWrapping.Wrap
            };
            panel.Children.Add(status);

            if (pending.Preview.Items.Count > 0)
            {
                var controls = new Grid { ColumnDefinitions = new ColumnDefinitions("*,86"), ColumnSpacing = 7 };
                var search = new TextBox
                {
                    Text = pending.SearchText,
                    Watermark = "Search",
                    FontSize = 10.5,
                    MinHeight = 28,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                controls.Children.Add(search);
                var limit = new TextBox
                {
                    Text = pending.LimitText,
                    Watermark = "Limit",
                    FontSize = 10.5,
                    MinHeight = 28,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(limit, 1);
                controls.Children.Add(limit);
                panel.Children.Add(controls);

                var rows = new StackPanel { Spacing = 5 };
                panel.Children.Add(rows);

                void RefreshPreview()
                {
                    var visibleItems = GetVisiblePreviewItems(pending);
                    var availableCount = GetAvailablePreviewItems(pending).Count;
                    status.Text = PreviewStatus(pending.Preview, visibleItems, availableCount);
                    if (queue is not null)
                    {
                        var queueCount = visibleItems.Count(item => item.Status == ImportQueueStatus.Queued);
                        queue.IsVisible = queueCount > 0;
                        queue.Content = queueCount == 1 ? "Queue 1" : $"Queue {queueCount}";
                    }

                    rows.Children.Clear();
                    if (visibleItems.Count == 0)
                    {
                        rows.Children.Add(new TextBlock
                        {
                            Text = "No matches.",
                            FontSize = 10.5,
                            Foreground = new SolidColorBrush(Color.Parse("#8996A3")),
                            Opacity = 0.72
                        });
                    }
                    else
                    {
                        foreach (var source in visibleItems.GroupBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase))
                            rows.Children.Add(CreatePreviewSourceCard(pending, source.Key, source, RefreshPreview));
                    }
                }

                search.TextChanged += (_, _) =>
                {
                    pending.SearchText = search.Text ?? string.Empty;
                    RefreshPreview();
                };
                limit.TextChanged += (_, _) =>
                {
                    pending.LimitText = limit.Text ?? string.Empty;
                    RefreshPreview();
                };
                RefreshPreview();
            }
        }

        card.Child = panel;
        return card;
    }

    private Control CreateSourceCard(ImportQueueSource source)
    {
        var card = new Border { Background = new SolidColorBrush(Color.Parse("#C8151B22")), BorderBrush = new SolidColorBrush(Color.Parse("#2D3D4B")), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5), Padding = new Thickness(10, 8) };
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(new TextBlock { Text = ShortUrl(source.SourceUrl), FontSize = 11, Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"), TextTrimming = TextTrimming.CharacterEllipsis });
        foreach (var item in source.Items)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
            row.Children.Add(CreateItemRow(item.Title, item.Status, QueueItemDetail(item), item.DurationSeconds));
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

    private static Control CreateItemRow(string title, ImportQueueStatus status, string detail, int? durationSeconds = null)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 7, Margin = new Thickness(0, 1) };
        row.Children.Add(new TextBlock
        {
            Text = durationSeconds is int seconds ? FormatTrackDuration(seconds) : "--:--",
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#798796")),
            MinWidth = 34,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        });
        var titleBlock = new TextBlock { Text = title, FontSize = 10.5, Foreground = new SolidColorBrush(Color.Parse(status is ImportQueueStatus.Queued or ImportQueueStatus.ReadyForReview ? "#DDE8F0" : "#8D9AA7")), TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(titleBlock, 1);
        row.Children.Add(titleBlock);
        var state = new TextBlock { Text = detail, FontSize = 9.5, Foreground = new SolidColorBrush(Color.Parse(StatusColor(status))), Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
        Grid.SetColumn(state, 2);
        row.Children.Add(state);
        return row;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _inputDebounceTimer.Stop();
        _queueElapsedTimer.Stop();
        foreach (var token in _checkingTokens.ToList())
            token.Cancel();
        IsVisible = false;
    }

    private void UpdateQueueElapsedTimer(
        IReadOnlyList<ImportQueueSource>? sources = null,
        IReadOnlyList<AnalysisQueueItem>? analysisItems = null)
    {
        var hasActiveItem = sources?.Any(source => source.Items.Any(IsActiveQueueItem))
                            ?? ImportQueueService.Current.GetSources().Any(source => source.Items.Any(IsActiveQueueItem));
        var hasActiveAnalysis = analysisItems?.Any(item => item.IsActive)
                                ?? BackgroundAnalysisService.Current.GetSnapshot().ActiveTrackId is not null;

        if (IsVisible && (hasActiveItem || hasActiveAnalysis))
            _queueElapsedTimer.Start();
        else
            _queueElapsedTimer.Stop();
    }

    private static bool IsActiveQueueItem(ImportQueueItem item) =>
        item.Status is ImportQueueStatus.Downloading or ImportQueueStatus.Analyzing;

    private static IReadOnlyList<ImportPreviewItem> GetVisiblePreviewItems(PendingImportPreview pending)
    {
        IEnumerable<ImportPreviewItem> items = GetAvailablePreviewItems(pending);

        if (int.TryParse(pending.LimitText.Trim(), out var limit) && limit >= 0)
            items = items.Take(limit);

        var search = pending.SearchText.Trim();
        if (search.Length > 0)
            items = items.Where(item =>
                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.CanonicalUrl.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (item.Detail?.Contains(search, StringComparison.OrdinalIgnoreCase) == true));

        return items.ToList();
    }

    private static IReadOnlyList<ImportPreviewItem> GetAvailablePreviewItems(PendingImportPreview pending) =>
        pending.Preview is null
            ? []
            : pending.Preview.Items
                .Where(item => !pending.RemovedCanonicalUrls.Contains(item.CanonicalUrl))
                .ToList();

    private static string QueueItemDetail(ImportQueueItem item)
    {
        if (!IsActiveQueueItem(item))
            return item.Detail ?? StatusLabel(item.Status);

        var phase = ImportQueueService.Current.GetActivePhase(item.Id);
        var elapsed = phase is null ? "0:00" : FormatElapsed(DateTime.UtcNow - phase.StartedAtUtc);
        var suffix = CleanPhaseDetail(item.Detail);
        return string.IsNullOrWhiteSpace(suffix)
            ? $"{StatusLabel(item.Status)} · {elapsed}"
            : $"{StatusLabel(item.Status)} · {elapsed} · {suffix}";
    }

    private static string CleanPhaseDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
            return string.Empty;

        var cleaned = detail
            .Replace("Downloading audio…", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Analyzing track…", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Checking download details…", "", StringComparison.OrdinalIgnoreCase)
            .Trim(' ', '·');
        return cleaned;
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
        ImportQueueStatus.Queued => "#C7D2AD",
        _ => "#8996A3"
    };

    private static string ShortUrl(string url) => url.Length > 78 ? url[..75] + "…" : url;
    private static string FormatBytes(long bytes) => bytes >= 1_000_000_000 ? $"{bytes / 1_000_000_000d:0.0} GB" : $"{bytes / 1_000_000d:0} MB";
    private static string FormatTrackDuration(int seconds)
    {
        var value = TimeSpan.FromSeconds(seconds);
        return value.TotalHours >= 1
            ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
            : $"{(int)value.TotalMinutes}:{value.Seconds:00}";
    }

    private static string FormatDuration(TimeSpan value) => value.TotalMinutes >= 1 ? $"{Math.Ceiling(value.TotalMinutes):0} min" : $"{Math.Max(1, Math.Round(value.TotalSeconds)):0} sec";
    private static string FormatElapsed(TimeSpan value) => value.TotalHours >= 1
        ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
        : $"{(int)value.TotalMinutes}:{value.Seconds:00}";

    private static IReadOnlyList<AnalysisQueueItem> GetAnalysisQueueItems()
    {
        var snapshot = BackgroundAnalysisService.Current.GetSnapshot();
        var tracks = MusicLibraryService.Current.GetTracks()
            .Where(track => track.Id == snapshot.ActiveTrackId || snapshot.PendingTrackIds.Contains(track.Id))
            .ToDictionary(track => track.Id);

        var items = new List<AnalysisQueueItem>();
        if (snapshot.ActiveTrackId is int activeTrackId && tracks.TryGetValue(activeTrackId, out var activeTrack))
            items.Add(new AnalysisQueueItem(activeTrack.Title, "analyzing", true));

        items.AddRange(snapshot.PendingTrackIds
            .Where(id => tracks.ContainsKey(id))
            .Select(id => new AnalysisQueueItem(
                tracks[id].Title,
                snapshot.ServerConnectionState == AnalysisServerConnectionState.Unreachable
                    ? "waiting for server"
                    : snapshot.IsWaitingForServerConfiguration ? "waiting for server setup" : "waiting",
                false)));
        return items;
    }

    private static Control CreateAnalysisQueueRow(AnalysisQueueItem item)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 1) };
        row.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse(item.IsActive ? "#E87878" : "#D89A9A")),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var state = new TextBlock
        {
            Text = item.Status,
            FontSize = 9.5,
            Foreground = new SolidColorBrush(Color.Parse(item.IsActive ? "#E87878" : "#A97878")),
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(state, 1);
        row.Children.Add(state);
        if (item.IsActive)
        {
            var cancel = new Button
            {
                Content = "Cancel",
                FontSize = 9,
                Padding = new Thickness(6, 2),
                Opacity = 0.78
            };
            cancel.Click += (_, _) => BackgroundAnalysisService.Current.CancelActiveAnalysis();
            Grid.SetColumn(cancel, 2);
            row.Children.Add(cancel);
        }
        return row;
    }

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
        public string SearchText { get; set; } = string.Empty;
        public string LimitText { get; set; } = string.Empty;
        public HashSet<string> RemovedCanonicalUrls { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record AnalysisQueueItem(string Title, string Status, bool IsActive);
}
