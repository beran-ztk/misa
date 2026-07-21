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

    public event Action? CloseRequested;
    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? PreviewRequested;
    public event Action? PreviewClosed;
    public event Action<int>? TrackChanged;

    public ChannelOverlay()
    {
        InitializeComponent();
        SetVideoFilter(showAll: false);
    }

    public void Open()
    {
        IsVisible = true;
        ChannelSidebar.IsVisible = false;
        RefreshChannels();
    }

    public void UpdateDownloadSummary()
    {
        UpdateStatus();
        var summary = ChannelDownloadService.Current.GetSummary();
        if (summary.Downloading > 0 || summary.Queued > 0)
            StatusText.Text += $" · {summary.Downloading} downloading · {summary.Queued} queued";
    }

    public void OnDownloadFinished(int videoId, MusicTrack? track, string? error)
    {
        var item = _currentVideos.FirstOrDefault(video => video.Id == videoId);
        item?.SetDownloadResult(track?.Id, error);
        UpdateDownloadSummary();
    }

    public void RefreshChannels()
    {
        var previousId = _selectedChannelId;
        _channels = MusicLibraryService.Current.GetChannelSubscriptions();
        ChannelList.ItemsSource = _channels;
        ChannelList.SelectedItem = _channels.FirstOrDefault(channel => channel.Id == previousId) ?? _channels.FirstOrDefault();
        UpdateStatus();
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
            UpdateStatus();
        }
    }

    private void OnChannelSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
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
        var uncheckedCount = _currentVideos.Count(video => !video.IsChecked);
        VideoSummaryText.Text = _showAllVideos
            ? $"{_currentVideos.Count} videos · {uncheckedCount} unchecked"
            : $"{uncheckedCount} unchecked videos";
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
        UpdateStatus();
        if (selectedId >= 0)
        {
            var uncheckedCount = _channels.FirstOrDefault(channel => channel.Id == selectedId)?.UncheckedCount ?? 0;
            var totalCount = _channels.FirstOrDefault(channel => channel.Id == selectedId)?.VideoCount ?? 0;
            VideoSummaryText.Text = $"{totalCount} videos · {uncheckedCount} unchecked";
        }
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

}

public sealed class ChannelVideoDisplay : INotifyPropertyChanged
{
    private bool _isChecked;
    private double _opacity;
    private double _checkOpacity;
    private IBrush _background = Brushes.Transparent;
    private IBrush _checkBackground = Brushes.Transparent;
    private IBrush _checkBorder = new SolidColorBrush(Color.Parse("#40515E"));
    private TextDecorationCollection? _textDecorations;
    private int? _trackId;
    private string _statusText = string.Empty;
    private bool _canPlay;
    private bool _canReview;
    private double _actionOpacity;

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
        CheckBackground = new SolidColorBrush(Color.Parse(value ? "#176486" : "#131A20"));
        CheckBorder = new SolidColorBrush(Color.Parse(value ? "#4AA9D1" : "#40515E"));
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
        StatusText = status switch
        {
            ChannelDownloadStatus.Ready => "Ready",
            ChannelDownloadStatus.Downloading => "Downloading…",
            ChannelDownloadStatus.Queued => "Queued",
            ChannelDownloadStatus.Failed => string.IsNullOrWhiteSpace(error) ? "Download failed" : "Failed",
            _ => "Auto-download off"
        };
        CanPlay = TrackId is not null;
        CanReview = !IsChecked && TrackId is not null;
        ActionOpacity = TrackId is not null && !IsChecked ? 0.78 : 0.3;
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
