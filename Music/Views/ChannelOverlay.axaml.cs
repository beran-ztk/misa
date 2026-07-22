using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ChannelOverlay : UserControl
{
    private CancellationTokenSource? _refreshCts;
    private List<ChannelSubscription> _channels = [];
    private List<ChannelVideoDisplay> _currentVideos = [];
    private int _selectedChannelId = -1;
    private bool _loadingVideos;
    private bool _showAllVideos;
    private bool _processingPastedUrl;
    private bool _refreshingChannelStates;
    private readonly Avalonia.Threading.DispatcherTimer _channelStateRefreshTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(750)
    };

    public event Action? CloseRequested;
    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? PreviewRequested;
    public event Action? PreviewClosed;
    public event Action<int>? TrackChanged;

    public ChannelOverlay()
    {
        InitializeComponent();
        _channelStateRefreshTimer.Tick += (_, _) =>
        {
            _channelStateRefreshTimer.Stop();
            RefreshChannelStates();
        };
        SetVideoFilter(showAll: false);
    }

    public void Open()
    {
        IsVisible = true;
        ChannelSidebar.IsVisible = false;
        GlobalMaxDurationBox.Text = MusicLibraryService.Current.GetChannelMaxDownloadDurationMinutes().ToString();
        RefreshChannels();
    }

    public void UpdateDownloadSummary()
    {
        UpdateStatus();
        var summary = ChannelDownloadService.Current.GetSummary();
        StatusText.Text += $" · Ready {summary.Ready} · Failed {summary.Failed} · " +
                           $"Queued {summary.Queued} · Downloading {summary.Downloading} · Skipped {summary.Skipped}";
        if (IsVisible)
        {
            _channelStateRefreshTimer.Stop();
            _channelStateRefreshTimer.Start();
        }
    }

    public void OnDownloadFinished(int videoId, MusicTrack? track, string? error)
    {
        var item = _currentVideos.FirstOrDefault(video => video.Id == videoId);
        item?.SetDownloadResult(track?.Id, error);
        UpdateVideoSummary();
        UpdateDownloadSummary();
    }

    public void RefreshChannels()
    {
        var previousId = _selectedChannelId;
        _channels = MusicLibraryService.Current.GetChannelSubscriptions();
        ChannelList.ItemsSource = _channels;
        ChannelList.SelectedItem = _channels.FirstOrDefault(channel => channel.Id == previousId) ?? _channels.FirstOrDefault();
        UpdateDownloadSummary();
        RefreshVideos();
    }

    private async void OnUrlTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_processingPastedUrl)
            return;

        var url = UrlBox.Text?.Trim();
        if (!LooksLikeYouTubeUrl(url))
            return;

        _processingPastedUrl = true;
        UrlBox.IsEnabled = false;
        try
        {
            await RunRefreshAsync(
                progress => MusicLibraryService.Current.AddOrRefreshChannelAsync(url!, progress, _refreshCts!.Token),
                "Channel added");
            UrlBox.Text = string.Empty;
        }
        finally
        {
            UrlBox.IsEnabled = true;
            _processingPastedUrl = false;
        }
    }

    private static bool LooksLikeYouTubeUrl(string? text)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.TrimStart('.');
        return (host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".youtube.com", StringComparison.OrdinalIgnoreCase))
               && uri.AbsolutePath.Length > 1;
    }

    private async void OnRefreshSelectedClicked(object? sender, RoutedEventArgs e)
    {
        if (ChannelList.SelectedItem is not ChannelSubscription channel)
            return;

        await RunRefreshAsync(
            progress => MusicLibraryService.Current.RefreshChannelAsync(channel, progress, _refreshCts!.Token),
            "Channel refreshed");
    }

    private void OnDeleteChannelClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ChannelSubscription channel)
            return;

        e.Handled = true;

        if (!MusicLibraryService.Current.DeleteChannel(channel.Id))
        {
            ToastRequested?.Invoke("Channel could not be deleted");
            return;
        }

        if (_selectedChannelId == channel.Id)
            _selectedChannelId = -1;
        RefreshChannels();
        ToastRequested?.Invoke("Channel deleted");
    }

    private async System.Threading.Tasks.Task RunRefreshAsync(
        Func<IProgress<string>, System.Threading.Tasks.Task<ChannelRefreshResult>> refresh,
        string successText)
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var progress = new Progress<string>(message => StatusText.Text = message);

        try
        {
            var result = await refresh(progress);
            if (!result.Success)
            {
                ToastRequested?.Invoke(result.Error ?? "Channel refresh failed");
                return;
            }

            RefreshChannels();
            ToastRequested?.Invoke(result.AddedCount > 0
                ? $"{successText}: {result.AddedCount} new videos"
                : $"{successText}: no new videos");
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            ToastRequested?.Invoke($"Channel refresh failed: {exception.Message}");
        }
        finally
        {
            UpdateDownloadSummary();
        }
    }

    private void OnChannelSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_refreshingChannelStates)
            return;
        _selectedChannelId = (ChannelList.SelectedItem as ChannelSubscription)?.Id ?? -1;
        SelectedChannelText.Text = (ChannelList.SelectedItem as ChannelSubscription)?.Name ?? "Channels";
        ChannelSidebar.IsVisible = false;
        PreviewClosed?.Invoke();
        RefreshVideos();
    }

    private void RefreshVideos()
    {
        if (_selectedChannelId < 0)
        {
            VideoList.ItemsSource = null;
            VideoSummaryText.Text = "No channel selected";
            return;
        }

        _currentVideos = MusicLibraryService.Current.GetChannelVideos(_selectedChannelId)
            .Select(video => new ChannelVideoDisplay(video))
            .ToList();

        var videos = _showAllVideos
            ? _currentVideos
            : _currentVideos.Where(video => !video.IsChecked).ToList();

        _loadingVideos = true;
        try
        {
            VideoList.ItemsSource = videos;
        }
        finally
        {
            _loadingVideos = false;
        }
        UpdateVideoSummary();
    }

    private void OnVideoCheckClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ChannelVideoDisplay item)
            return;
        if (_loadingVideos || !item.CanReview)
            return;

        var track = MusicLibraryService.Current.ConfirmChannelVideo(item.Id);
        if (track is null)
        {
            ToastRequested?.Invoke("Audio is not downloaded yet");
            return;
        }
        item.IsChecked = true;
        TrackChanged?.Invoke(track.Id);
        RefreshChannelSummaries();
        RefreshVideos();
        ToastRequested?.Invoke("Accepted · analysis queued");
    }

    private void OnVideoSkipClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ChannelVideoDisplay item || !item.CanReview)
            return;

        var track = MusicLibraryService.Current.SkipChannelVideo(item.Id);
        if (track is null)
        {
            ToastRequested?.Invoke("Audio is not downloaded yet");
            return;
        }
        item.IsChecked = true;
        PreviewClosed?.Invoke();
        TrackChanged?.Invoke(track.Id);
        RefreshChannelSummaries();
        RefreshVideos();
        ToastRequested?.Invoke("Skipped · audio kept");
    }

    private void OnVideoPlayClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ChannelVideoDisplay item
            || item.TrackId is not int trackId || MusicLibraryService.Current.GetTrackById(trackId) is not { } track)
            return;
        PreviewRequested?.Invoke(track);
    }

    private void OnAutoDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not ChannelSubscription channel)
            return;
        e.Handled = true;
        MusicLibraryService.Current.SetChannelAutoDownload(channel.Id, checkBox.IsChecked == true);
        RefreshChannels();
        ChannelSidebar.IsVisible = true;
    }

    private void OnGlobalMaxDurationLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (!int.TryParse(textBox.Text, out var minutes) || minutes < 1)
        {
            textBox.Text = MusicLibraryService.Current.GetChannelMaxDownloadDurationMinutes().ToString();
            ToastRequested?.Invoke("Enter a duration of at least 1 minute");
            return;
        }

        minutes = Math.Clamp(minutes, 1, 24 * 60);
        MusicLibraryService.Current.SetGlobalChannelMaxDownloadDuration(minutes);
        textBox.Text = minutes.ToString();
        RefreshVideos();
        UpdateDownloadSummary();
        ToastRequested?.Invoke($"Global download limit set to {minutes} min");
    }

    private void OnSidebarToggleClicked(object? sender, RoutedEventArgs e) =>
        ChannelSidebar.IsVisible = !ChannelSidebar.IsVisible;

    private void OnPendingFilterPressed(object? sender, PointerPressedEventArgs e)
    {
        SetVideoFilter(showAll: false);
        e.Handled = true;
    }

    private void OnAllFilterPressed(object? sender, PointerPressedEventArgs e)
    {
        SetVideoFilter(showAll: true);
        e.Handled = true;
    }

    private void SetVideoFilter(bool showAll)
    {
        _showAllVideos = showAll;
        if (VideoFilterSelectionIndicator.RenderTransform is TranslateTransform transform)
            transform.X = showAll ? 72 : 0;
        VideoFilterSelectionIndicator.CornerRadius = showAll
            ? new Avalonia.CornerRadius(0, 5, 5, 0)
            : new Avalonia.CornerRadius(5, 0, 0, 5);
        PendingFilterText.Foreground = new SolidColorBrush(Color.Parse(showAll ? "#B8C5CE" : "#FFFFFF"));
        AllFilterText.Foreground = new SolidColorBrush(Color.Parse(showAll ? "#FFFFFF" : "#B8C5CE"));
        RefreshVideos();
    }

    private async void OnCopyVideoUrlClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ChannelVideoDisplay item)
            return;

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            ToastRequested?.Invoke("Clipboard is not available");
            return;
        }

        await clipboard.SetTextAsync(item.CanonicalUrl);
        ToastRequested?.Invoke("Video URL copied");
    }

    private void RefreshChannelSummaries()
    {
        var selectedId = _selectedChannelId;
        _channels = MusicLibraryService.Current.GetChannelSubscriptions();
        ChannelList.ItemsSource = _channels;
        ChannelList.SelectedItem = _channels.FirstOrDefault(channel => channel.Id == selectedId);
        UpdateDownloadSummary();
        if (selectedId >= 0) UpdateVideoSummary();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _refreshCts?.Cancel();
        CloseRequested?.Invoke();
    }

    private void UpdateStatus()
    {
        var uncheckedCount = _channels.Sum(channel => channel.UncheckedCount);
        StatusText.Text = _channels.Count == 0
            ? "Add a YouTube channel URL to start tracking videos."
            : $"{_channels.Count} channels · {uncheckedCount} unchecked videos";
    }

    private void RefreshChannelStates()
    {
        if (!IsVisible)
            return;

        var selectedId = _selectedChannelId;
        var sidebarVisible = ChannelSidebar.IsVisible;
        _refreshingChannelStates = true;
        try
        {
            _channels = MusicLibraryService.Current.GetChannelSubscriptions();
            ChannelList.ItemsSource = _channels;
            ChannelList.SelectedItem = _channels.FirstOrDefault(channel => channel.Id == selectedId)
                                       ?? _channels.FirstOrDefault();
        }
        finally
        {
            _refreshingChannelStates = false;
            ChannelSidebar.IsVisible = sidebarVisible;
        }
    }

    private void UpdateVideoSummary()
    {
        if (_selectedChannelId < 0)
        {
            VideoSummaryText.Text = "No channel selected";
            return;
        }

        var uncheckedCount = _currentVideos.Count(video => !video.IsChecked);
        var ready = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Ready);
        var failed = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Failed);
        var queued = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Queued);
        var downloading = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Downloading);
        var skipped = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Skipped);
        VideoSummaryText.Text = $"{_currentVideos.Count} tracks · Ready {ready} · Failed {failed} · " +
                                $"Queued {queued} · Downloading {downloading} · Skipped {skipped} · {uncheckedCount} unchecked";
    }

}

public sealed class ChannelVideoDisplay : INotifyPropertyChanged
{
    private bool _isChecked;
    private double _opacity;
    private double _checkOpacity;
    private IBrush _background = Brushes.Transparent;
    private IBrush _checkBackground = Brushes.Transparent;
    private IBrush _checkBorder = ThemeResources.Brush("Theme.Brush.Border");
    private TextDecorationCollection? _textDecorations;
    private int? _trackId;
    private string _statusText = string.Empty;
    private bool _canPlay;
    private bool _canReview;
    private double _actionOpacity;
    private ChannelDownloadStatus _downloadStatus;
    private string _downloadErrorSummary = string.Empty;
    private string _downloadErrorDetails = string.Empty;
    private bool _hasDownloadError;

    public ChannelVideoDisplay(ChannelVideo video)
    {
        Id = video.Id;
        CanonicalUrl = video.CanonicalUrl;
        Title = video.Title;
        DurationText = video.DurationSeconds is int seconds ? FormatDuration(seconds) : "";
        _trackId = video.TrackId;
        SetDownloadState(video.DownloadStatus, video.DownloadError);
        SetChecked(video.IsChecked);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }
    public string CanonicalUrl { get; }
    public string Title { get; }
    public bool IsChecked
    {
        get => _isChecked;
        set => SetChecked(value);
    }
    public string DurationText { get; }
    public int? TrackId
    {
        get => _trackId;
        private set => SetField(ref _trackId, value);
    }
    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }
    public bool CanPlay
    {
        get => _canPlay;
        private set => SetField(ref _canPlay, value);
    }
    public bool CanReview
    {
        get => _canReview;
        private set => SetField(ref _canReview, value);
    }
    public double ActionOpacity
    {
        get => _actionOpacity;
        private set => SetField(ref _actionOpacity, value);
    }
    public ChannelDownloadStatus DownloadStatus
    {
        get => _downloadStatus;
        private set => SetField(ref _downloadStatus, value);
    }
    public string DownloadErrorSummary
    {
        get => _downloadErrorSummary;
        private set => SetField(ref _downloadErrorSummary, value);
    }
    public string DownloadErrorDetails
    {
        get => _downloadErrorDetails;
        private set => SetField(ref _downloadErrorDetails, value);
    }
    public bool HasDownloadError
    {
        get => _hasDownloadError;
        private set => SetField(ref _hasDownloadError, value);
    }
    public double Opacity
    {
        get => _opacity;
        private set => SetField(ref _opacity, value);
    }
    public IBrush Background
    {
        get => _background;
        private set => SetField(ref _background, value);
    }
    public double CheckOpacity
    {
        get => _checkOpacity;
        private set => SetField(ref _checkOpacity, value);
    }
    public IBrush CheckBackground
    {
        get => _checkBackground;
        private set => SetField(ref _checkBackground, value);
    }
    public IBrush CheckBorder
    {
        get => _checkBorder;
        private set => SetField(ref _checkBorder, value);
    }
    public TextDecorationCollection? TextDecorations
    {
        get => _textDecorations;
        private set => SetField(ref _textDecorations, value);
    }

    private void SetChecked(bool value)
    {
        SetField(ref _isChecked, value, nameof(IsChecked));
        Opacity = value ? 0.45 : 1;
        Background = value
            ? new SolidColorBrush(Color.Parse("#22192027"))
            : new SolidColorBrush(Color.Parse("#10203422"));
        CheckBackground = ThemeResources.Brush(value ? "Theme.Brush.Success" : "Theme.Brush.Surface");
        CheckBorder = ThemeResources.Brush(value ? "Theme.Brush.Accent" : "Theme.Brush.Border");
        CheckOpacity = value ? 1 : 0.58;
        TextDecorations = value ? Avalonia.Media.TextDecorations.Strikethrough : null;
        CanReview = !value && TrackId is not null;
        ActionOpacity = TrackId is not null && !value ? 0.78 : 0.3;
    }

    public void SetDownloadResult(int? trackId, string? error)
    {
        TrackId = trackId;
        SetDownloadState(trackId is not null ? ChannelDownloadStatus.Ready : ChannelDownloadStatus.Failed, error);
        SetChecked(IsChecked);
    }

    private void SetDownloadState(ChannelDownloadStatus status, string? error)
    {
        DownloadStatus = status;
        StatusText = status switch
        {
            ChannelDownloadStatus.Ready => "Ready",
            ChannelDownloadStatus.Downloading => "Downloading…",
            ChannelDownloadStatus.Queued => "Queued",
            ChannelDownloadStatus.Failed => string.IsNullOrWhiteSpace(error) ? "Download failed" : "Failed",
            ChannelDownloadStatus.Skipped => "Skipped · duration filter",
            _ => "Auto-download off"
        };
        SetDownloadError(status == ChannelDownloadStatus.Failed ? error : null);
        CanPlay = TrackId is not null;
        CanReview = !IsChecked && TrackId is not null;
        ActionOpacity = TrackId is not null && !IsChecked ? 0.78 : 0.3;
    }

    private void SetDownloadError(string? error)
    {
        var details = error?.Trim() ?? string.Empty;
        DownloadErrorDetails = details.Length > 4000 ? details[..4000] + "…" : details;
        var lines = details.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var summary = lines.LastOrDefault(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                      ?? lines.LastOrDefault()
                      ?? string.Empty;
        DownloadErrorSummary = summary.Length > 260 ? summary[..260] + "…" : summary;
        HasDownloadError = DownloadErrorSummary.Length > 0;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatDuration(int seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}"
            : $"{time.Minutes:D2}:{time.Seconds:D2}";
    }

}
