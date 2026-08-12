using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Resona.Services;

namespace Resona.Views;

public partial class ActivityCenter : UserControl
{
    private static readonly IBrush RunningBrush = Brush("#78AEE8");
    private static readonly IBrush QueuedBrush = Brush("#9AA8B5");
    private static readonly IBrush CompletedBrush = Brush("#79C994");
    private static readonly IBrush FailedBrush = Brush("#E87878");
    private static readonly IBrush CanceledBrush = Brush("#8A929B");
    private bool _subscribed;

    public ActivityCenter()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => Subscribe();
        DetachedFromVisualTree += (_, _) => Unsubscribe();
    }

    public event Action? CloseRequested;
    public event Action<ActivityCenterSummary>? SummaryChanged;
    public ActivityCenterSummary CurrentSummary { get; private set; } = new(0, 0, 0);

    public void Open()
    {
        ApplySnapshot(BackgroundJobService.Current.GetSnapshot());
        IsVisible = true;
    }

    public void Refresh() => ApplySnapshot(BackgroundJobService.Current.GetSnapshot());

    private void Subscribe()
    {
        if (_subscribed)
            return;
        _subscribed = true;
        BackgroundJobService.Current.SnapshotChanged += OnSnapshotChanged;
        ApplySnapshot(BackgroundJobService.Current.GetSnapshot());
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;
        _subscribed = false;
        BackgroundJobService.Current.SnapshotChanged -= OnSnapshotChanged;
    }

    private void OnSnapshotChanged(BackgroundJobServiceSnapshot snapshot)
    {
        if (Dispatcher.UIThread.CheckAccess())
            ApplySnapshot(snapshot);
        else
            Dispatcher.UIThread.Post(() =>
            {
                if (_subscribed)
                    ApplySnapshot(snapshot);
            });
    }

    private void ApplySnapshot(BackgroundJobServiceSnapshot snapshot)
    {
        var running = snapshot.Jobs.Count(job => job.State == BackgroundJobState.Running);
        var queued = snapshot.Jobs.Count(job => job.State == BackgroundJobState.Queued);
        var failed = snapshot.Jobs.Count(job => job.State == BackgroundJobState.Failed);
        CurrentSummary = new ActivityCenterSummary(running, queued, failed);
        SummaryChanged?.Invoke(CurrentSummary);

        LimitText.Text = $"YouTube · {running}/{snapshot.MaximumConcurrency} active";
        SummaryText.Text = Summary(running, queued, failed);
        PausedText.IsVisible = snapshot.BackgroundJobsPaused;
        PauseBackgroundIcon.IsVisible = !snapshot.BackgroundJobsPaused;
        ResumeBackgroundIcon.IsVisible = snapshot.BackgroundJobsPaused;
        ToolTip.SetTip(PauseBackgroundButton, snapshot.BackgroundJobsPaused
            ? "Resume background YouTube jobs"
            : "Pause background YouTube jobs");

        var finishedCount = snapshot.Jobs.Count(job => IsFinished(job.State));
        ClearHistoryButton.IsEnabled = finishedCount > 0;
        ClearHistoryButton.Opacity = finishedCount > 0 ? 1 : 0.35;
        EmptyText.IsVisible = snapshot.Jobs.Count == 0;
        JobRows.Children.Clear();

        AddSection("ACTIVE", snapshot.Jobs.Where(job => job.State == BackgroundJobState.Running), snapshot);
        AddSection("WAITING", snapshot.Jobs.Where(job => job.State == BackgroundJobState.Queued), snapshot);
        AddSection(
            "RECENT",
            snapshot.Jobs
                .Where(job => IsFinished(job.State))
                .OrderByDescending(job => job.FinishedAtUtc)
                .Take(30),
            snapshot);
    }

    private void AddSection(
        string title,
        IEnumerable<BackgroundJobSnapshot> jobs,
        BackgroundJobServiceSnapshot serviceSnapshot)
    {
        var items = jobs.ToList();
        if (items.Count == 0)
            return;

        JobRows.Children.Add(new TextBlock
        {
            Text = $"{title} · {items.Count}",
            FontSize = 9,
            FontWeight = FontWeight.SemiBold,
            LetterSpacing = 0.8,
            Opacity = 0.42,
            Margin = new Thickness(0, 5, 0, 1)
        });
        foreach (var job in items)
            JobRows.Children.Add(CreateJobRow(job, serviceSnapshot));
    }

    private static Control CreateJobRow(
        BackgroundJobSnapshot job,
        BackgroundJobServiceSnapshot serviceSnapshot)
    {
        var row = new Border
        {
            Background = job.State == BackgroundJobState.Failed ? Brush("#241E1517") : Brushes.Transparent,
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(7, 6)
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            ColumnSpacing = 8
        };
        grid.Children.Add(new Border
        {
            Width = 6,
            Height = 6,
            CornerRadius = new CornerRadius(3),
            Background = StateBrush(job.State),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 0, 0)
        });

        var content = new StackPanel { Spacing = 2 };
        content.Children.Add(new TextBlock
        {
            Text = job.Title,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        content.Children.Add(new TextBlock
        {
            Text = $"{job.Source} · {KindLabel(job.Kind)} · {TimeLabel(job)}",
            FontSize = 9,
            Foreground = Brush("#8795A3"),
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var detail = Detail(job, serviceSnapshot);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            content.Children.Add(new TextBlock
            {
                Text = detail,
                FontSize = 9.5,
                Foreground = job.State == BackgroundJobState.Failed ? FailedBrush : Brush("#ABB6C0"),
                TextWrapping = job.State == BackgroundJobState.Failed ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = job.State == BackgroundJobState.Failed ? 2 : 1
            });
        }
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var state = new TextBlock
        {
            Text = StateLabel(job, serviceSnapshot),
            FontSize = 9,
            Foreground = StateBrush(job.State),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 2, 0, 0)
        };
        Grid.SetColumn(state, 2);
        grid.Children.Add(state);

        if (job.State is BackgroundJobState.Queued or BackgroundJobState.Running)
        {
            var cancel = new Button
            {
                Content = "×",
                Width = 24,
                Height = 22,
                Padding = new Thickness(0),
                FontSize = 12,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Opacity = 0.62,
                IsEnabled = !job.Detail.Equals("Canceling…", StringComparison.Ordinal)
            };
            ToolTip.SetTip(cancel, "Cancel this execution");
            cancel.Click += (_, _) => BackgroundJobService.Current.Cancel(job.Id);
            Grid.SetColumn(cancel, 3);
            grid.Children.Add(cancel);
        }

        row.Child = grid;
        return row;
    }

    private void OnPauseBackgroundClicked(object? sender, RoutedEventArgs e)
    {
        var snapshot = BackgroundJobService.Current.GetSnapshot();
        BackgroundJobService.Current.SetBackgroundJobsPaused(!snapshot.BackgroundJobsPaused);
    }

    private void OnClearHistoryClicked(object? sender, RoutedEventArgs e) =>
        BackgroundJobService.Current.ClearFinishedJobs();

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        IsVisible = false;
        CloseRequested?.Invoke();
    }

    private static string Summary(int running, int queued, int failed)
    {
        var parts = new List<string>();
        if (running > 0) parts.Add($"{running} active");
        if (queued > 0) parts.Add($"{queued} waiting");
        if (failed > 0) parts.Add($"{failed} failed");
        return parts.Count == 0 ? "No current YouTube activity" : string.Join(" · ", parts);
    }

    private static string Detail(BackgroundJobSnapshot job, BackgroundJobServiceSnapshot snapshot)
    {
        if (job.State == BackgroundJobState.Failed)
            return job.Error ?? job.Detail;
        if (job.State == BackgroundJobState.Queued
            && snapshot.BackgroundJobsPaused
            && job.Priority == BackgroundJobPriority.Background)
            return "Waiting while background work is paused";
        return job.Detail is "Completed" or "Waiting" ? string.Empty : job.Detail;
    }

    private static string StateLabel(BackgroundJobSnapshot job, BackgroundJobServiceSnapshot snapshot) =>
        job.State switch
        {
            BackgroundJobState.Queued when snapshot.BackgroundJobsPaused
                && job.Priority == BackgroundJobPriority.Background => "paused",
            BackgroundJobState.Queued => "waiting",
            BackgroundJobState.Running when job.Detail == "Canceling…" => "canceling",
            BackgroundJobState.Running => "running",
            BackgroundJobState.Completed => "done",
            BackgroundJobState.Failed => "failed",
            BackgroundJobState.Canceled => "canceled",
            _ => job.State.ToString().ToLowerInvariant()
        };

    private static string KindLabel(BackgroundJobKind kind) => kind switch
    {
        BackgroundJobKind.YouTubeDownload => "download",
        BackgroundJobKind.YouTubeMetadata => "metadata",
        BackgroundJobKind.YouTubePlaylist => "playlist",
        BackgroundJobKind.YouTubeChannelRefresh => "channel",
        _ => "YouTube"
    };

    private static string TimeLabel(BackgroundJobSnapshot job)
    {
        var timestamp = job.FinishedAtUtc ?? job.StartedAtUtc ?? job.CreatedAtUtc;
        var elapsed = DateTime.UtcNow - timestamp;
        if (elapsed < TimeSpan.FromMinutes(1)) return "now";
        if (elapsed < TimeSpan.FromHours(1)) return $"{Math.Max(1, (int)elapsed.TotalMinutes)}m ago";
        if (elapsed < TimeSpan.FromDays(1)) return $"{Math.Max(1, (int)elapsed.TotalHours)}h ago";
        return timestamp.ToLocalTime().ToString("dd.MM.");
    }

    private static bool IsFinished(BackgroundJobState state) => state is
        BackgroundJobState.Completed or BackgroundJobState.Failed or BackgroundJobState.Canceled;

    private static IBrush StateBrush(BackgroundJobState state) => state switch
    {
        BackgroundJobState.Running => RunningBrush,
        BackgroundJobState.Queued => QueuedBrush,
        BackgroundJobState.Completed => CompletedBrush,
        BackgroundJobState.Failed => FailedBrush,
        BackgroundJobState.Canceled => CanceledBrush,
        _ => QueuedBrush
    };

    private static SolidColorBrush Brush(string value) => new(Color.Parse(value));
}

public sealed record ActivityCenterSummary(int Running, int Queued, int Failed)
{
    public int CurrentCount => Running + Queued;
}
