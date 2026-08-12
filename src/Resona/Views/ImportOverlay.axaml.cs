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

    public void SetAtmosphereColors(Color primary, Color secondary)
    {
        if (ImportAtmosphereTint.Fill is not LinearGradientBrush gradient
            || gradient.GradientStops.Count < 2)
            return;

        gradient.GradientStops[0].Color = primary;
        gradient.GradientStops[1].Color = secondary;
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
        QueueHeaderText.Text = queueCount > 10 ? $"CURRENT QUEUE ({queueCount})" : "CURRENT QUEUE";
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
        return $"{found} · {queued} new · {existing} already in library"
            + (duplicates > 0 ? $" · {duplicates} duplicates" : string.Empty)
            + (unavailable > 0 ? $" · {unavailable} unavailable" : string.Empty);
    }

    private static Control CreatePreviewItemRow(PendingImportPreview pending, ImportPreviewItem item,
        Action refreshPreview)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 2) };
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse(item.Status is ImportQueueStatus.Queued or ImportQueueStatus.ReadyForReview ? "#DDE8F0" : "#8D9AA7")),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        row.Children.Add(title);
        var state = new TextBlock
        {
            Text = PreviewItemState(item),
            FontSize = 9.5,
            Foreground = new SolidColorBrush(Color.Parse(PreviewItemStateColor(item))),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        Grid.SetColumn(state, 1);
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
        Grid.SetColumn(remove, 2);
        row.Children.Add(remove);
        return row;
    }

    private Control CreatePendingPreviewCard(PendingImportPreview pending)
    {
        var card = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0, 2)
        };
        var panel = new StackPanel { Spacing = 5 };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 7 };
        var headerTitle = new TextBlock
        {
            Text = pending.IsChecking ? "Checking link…" : "Checked link",
            FontSize = 10.5,
            Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        header.Children.Add(headerTitle);
        Button? queue = null;
        if (pending.Preview is { } preview)
        {
            queue = new Button
            {
                Content = CreateSvgIcon("/Assets/download.svg", 14),
                Width = 28,
                Height = 26,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };
            ToolTip.SetTip(queue, "Add checked tracks to the download queue");
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
            var info = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 8
            };
            var status = new TextBlock
            {
                FontSize = 10.5,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            info.Children.Add(status);
            var estimates = new TextBlock
            {
                FontSize = 10.5,
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
                Opacity = 0.82,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(estimates, 1);
            info.Children.Add(estimates);
            panel.Children.Add(info);

            if (pending.Preview.Items.Count > 0)
            {
                var showLargeListControls = GetAvailablePreviewItems(pending).Count > 20;
                var controls = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,86"),
                    ColumnSpacing = 7,
                    IsVisible = showLargeListControls
                };
                var search = new TextBox
                {
                    Text = pending.SearchText,
                    Watermark = "Search",
                    FontSize = 10.5,
                    MinHeight = 28,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                search.Classes.Add("compact-search");
                controls.Children.Add(search);
                var limit = new TextBox
                {
                    Text = pending.LimitText,
                    Watermark = "Limit",
                    FontSize = 10.5,
                    MinHeight = 28,
                    VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                limit.Classes.Add("compact-search");
                Grid.SetColumn(limit, 1);
                controls.Children.Add(limit);
                panel.Children.Add(controls);

                var rows = new StackPanel { Spacing = 5 };
                panel.Children.Add(rows);

                void RefreshPreview()
                {
                    var visibleItems = GetVisiblePreviewItems(pending);
                    var availableCount = GetAvailablePreviewItems(pending).Count;
                    var isSingle = visibleItems.Count == 1;
                    headerTitle.Text = isSingle ? visibleItems[0].Title : $"{visibleItems.Count} checked tracks";
                    status.Text = isSingle
                        ? PreviewItemState(visibleItems[0])
                        : PreviewStatus(pending.Preview, visibleItems, availableCount);
                    status.Foreground = new SolidColorBrush(Color.Parse(isSingle
                        ? PreviewItemStateColor(visibleItems[0])
                        : visibleItems.Any(item => item.Status == ImportQueueStatus.Queued) ? "#82D99A" : "#E87878"));
                    estimates.Text = PreviewEstimates(pending.Preview, visibleItems);
                    if (queue is not null)
                    {
                        var queueCount = visibleItems.Count(item => item.Status == ImportQueueStatus.Queued);
                        queue.IsVisible = queueCount > 0;
                        ToolTip.SetTip(queue, queueCount == 1
                            ? "Add track to the download queue"
                            : $"Add {queueCount} tracks to the download queue");
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
                        if (!isSingle)
                        {
                            foreach (var item in visibleItems)
                                rows.Children.Add(CreatePreviewItemRow(pending, item, RefreshPreview));
                        }
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
        var panel = new StackPanel { Spacing = 4 };
        foreach (var item in source.Items)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 2) };
            var title = new TextBlock
            {
                Text = item.Title,
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.Parse(item.Status is ImportQueueStatus.Queued or ImportQueueStatus.ReadyForReview ? "#DDE8F0" : "#8D9AA7")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            row.Children.Add(title);
            var state = new TextBlock
            {
                Text = QueueItemDetail(item),
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.Parse(StatusColor(item.Status))),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(state, 1);
            row.Children.Add(state);
            if (item.Status is ImportQueueStatus.Queued or ImportQueueStatus.Failed)
            {
                var remove = new Button
                {
                    Content = "×",
                    FontSize = 12,
                    Padding = new Thickness(5, 0),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Opacity = 0.62
                };
                ToolTip.SetTip(remove, "Remove from queue");
                remove.Click += (_, _) =>
                {
                    if (ImportQueueService.Current.RemoveQueuedItem(item.Id)) RefreshQueue();
                };
                Grid.SetColumn(remove, 2);
                row.Children.Add(remove);
            }
            panel.Children.Add(row);
        }
        return panel;
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
            ? elapsed
            : $"{elapsed} · {suffix}";
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

    private static string PreviewItemState(ImportPreviewItem item) => item.Status switch
    {
        ImportQueueStatus.Queued => "Track is new",
        ImportQueueStatus.Skipped when item.Detail?.Equals("Already in library", StringComparison.OrdinalIgnoreCase) == true
            => "Track already exists",
        ImportQueueStatus.Skipped => item.Detail ?? "Track skipped",
        ImportQueueStatus.Failed => item.Detail ?? "Track unavailable",
        _ => item.Detail ?? "Ready"
    };

    private static string PreviewItemStateColor(ImportPreviewItem item) => item.Status switch
    {
        ImportQueueStatus.Queued => "#82D99A",
        ImportQueueStatus.Skipped or ImportQueueStatus.Failed => "#E87878",
        _ => "#DDE8F0"
    };

    private static string PreviewEstimates(ImportPreview preview, IReadOnlyList<ImportPreviewItem> visibleItems)
    {
        if (visibleItems.Count == 0 || !visibleItems.Any(item => item.Status == ImportQueueStatus.Queued))
            return string.Empty;

        var includeAggregate = visibleItems.Count == preview.Items.Count;
        return (includeAggregate && preview.EstimatedDownloadTime is TimeSpan download
                ? $"Download {FormatDuration(download)}"
                : string.Empty)
            + (includeAggregate && preview.EstimatedAnalysisTime is TimeSpan analysis
                ? $" · Analysis {FormatDuration(analysis)}"
                : string.Empty);
    }

    private static Avalonia.Svg.Skia.Svg CreateSvgIcon(string path, double size) => new(new Uri("avares://Resona/"))
    {
        Path = path,
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        Opacity = 0.82,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
    };

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
