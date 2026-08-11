using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Resona.Models;
using Resona.Services;

namespace Resona.Views;

public partial class ChannelOverlay : UserControl
{
    private enum ChannelVideoFilter { New, Ready, InLibrary, Issues, All }

    private CancellationTokenSource? _refreshCts;
    private int? _refreshChannelId;
    private string _refreshMessage = string.Empty;
    private List<ChannelHubItem> _hubChannels = [];
    private ChannelHubItem? _selectedHubChannel;
    private List<ChannelVideoDisplay> _currentVideos = [];
    private int _selectedChannelId = -1;
    private bool _detailOpenedFromInbox;
    private bool _loadingVideos;
    private ChannelVideoFilter _videoFilter = ChannelVideoFilter.New;
    private readonly Dictionary<int, ChannelVideoFilter> _videoFiltersByChannel = [];
    private readonly Dictionary<int, string> _videoSearchByChannel = [];
    private bool _processingPastedUrl;
    private int? _activePreviewTrackId;
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
        SetVideoFilter(ChannelVideoFilter.New, refresh: false);
    }

    public void Open()
    {
        IsVisible = true;
        HubView.IsVisible = true;
        DetailView.IsVisible = false;
        InboxView.IsVisible = false;
        _detailOpenedFromInbox = false;
        RefreshChannels();
    }

    public void UpdateDownloadSummary()
    {
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
        ApplyVideoView();
        UpdateDownloadSummary();
    }

    public void OnMetadataUpdated(int channelId, int videoId)
    {
        if (_selectedChannelId != channelId || !DetailView.IsVisible)
            return;
        _channelStateRefreshTimer.Stop();
        _channelStateRefreshTimer.Start();
    }

    public void RefreshChannels()
    {
        _hubChannels = MusicLibraryService.Current.GetChannelHubItems();
        ApplyHubFilter();
        RefreshInboxBadge();

        if (_selectedChannelId >= 0)
        {
            _selectedHubChannel = _hubChannels.FirstOrDefault(channel => channel.Id == _selectedChannelId);
            if (DetailView.IsVisible && _selectedHubChannel is not null)
            {
                UpdateDetailHeader();
                RefreshVideos();
            }
        }
    }

    public void SetAtmosphereColors(Color primary, Color secondary)
    {
        if (ChannelAtmosphereTint.Fill is not LinearGradientBrush gradient
            || gradient.GradientStops.Count < 2)
            return;

        gradient.GradientStops[0].Color = primary;
        gradient.GradientStops[1].Color = secondary;
    }

    private void ApplyHubFilter()
    {
        var search = ChannelSearchBox.Text?.Trim() ?? string.Empty;
        IEnumerable<ChannelHubItem> matching = _hubChannels;
        if (search.Length > 0)
            matching = matching.Where(channel =>
                channel.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || channel.TopTracks.Any(title => title.Contains(search, StringComparison.OrdinalIgnoreCase)));

        var visible = matching.ToList();
        var following = visible
            .Where(channel => channel.IsFollowed)
            .OrderByDescending(channel => channel.UncheckedVideoCount)
            .ThenByDescending(channel => channel.RecommendationScore)
            .ThenBy(channel => channel.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var suggested = visible
            .Where(channel => !channel.IsFollowed && channel.LocalTrackCount > 0)
            .OrderByDescending(channel => channel.RecommendationScore)
            .ThenByDescending(channel => channel.LocalTrackCount)
            .Take(6)
            .ToList();
        var all = visible
            .OrderByDescending(channel => channel.IsFollowed)
            .ThenBy(channel => channel.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        FollowingItems.ItemsSource = following;
        SuggestedItems.ItemsSource = suggested;
        AllChannelItems.ItemsSource = all;
        EmptyFollowingText.IsVisible = following.Count == 0;
        SuggestedSection.IsVisible = suggested.Count > 0;
        EmptySearchText.IsVisible = all.Count == 0;
        EmptySearchText.Text = search.Length > 0
            ? "No channels match this search."
            : "No library channels yet. Add a YouTube channel to get started.";
        FollowingCountText.Text = following.Count.ToString();
        AllChannelCountText.Text = all.Count.ToString();

        var totalFollowing = _hubChannels.Count(channel => channel.IsFollowed);
        var newVideos = _hubChannels.Where(channel => channel.IsFollowed).Sum(channel => channel.UncheckedVideoCount);
        HubSummaryText.Text = $"{_hubChannels.Count} library channels · {totalFollowing} following" +
                              (newVideos > 0 ? $" · {newVideos} new videos" : string.Empty);
    }

    private void OnChannelSearchChanged(object? sender, TextChangedEventArgs e) => ApplyHubFilter();

    private void OnAddChannelToggleClicked(object? sender, RoutedEventArgs e)
    {
        AddChannelPanel.IsVisible = !AddChannelPanel.IsVisible;
        if (AddChannelPanel.IsVisible)
            UrlBox.Focus();
    }

    private void OnOpenChannelClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: ChannelHubItem channel })
            return;

        OpenChannelDetail(channel);
        e.Handled = true;
    }

    private void OpenChannelDetail(ChannelHubItem channel, bool fromInbox = false)
    {
        _selectedHubChannel = channel;
        _selectedChannelId = channel.Id;
        _detailOpenedFromInbox = fromInbox;
        VideoSearchBox.Text = _videoSearchByChannel.GetValueOrDefault(channel.Id, string.Empty);
        SetVideoFilter(
            _videoFiltersByChannel.GetValueOrDefault(channel.Id, ChannelVideoFilter.New),
            refresh: false);
        HubView.IsVisible = false;
        InboxView.IsVisible = false;
        DetailView.IsVisible = true;
        PreviewClosed?.Invoke();
        UpdateDetailHeader();
        RefreshVideos();
        if (channel.IsFollowed
            && string.IsNullOrWhiteSpace(channel.LastCheckedAt)
            && !string.IsNullOrWhiteSpace(channel.SourceUrl))
        {
            _ = RefreshChannelDiscoveryAsync(channel, "Channel loaded");
        }
        else
        {
            ChannelMetadataService.Current.RequestChannel(channel.Id, 20);
        }
    }

    private void UpdateDetailHeader()
    {
        if (_selectedHubChannel is not { } channel)
            return;

        SelectedChannelText.Text = channel.Name;
        DetailMonogramText.Text = channel.Monogram;
        var localStatus = channel.HasNewVideos
            ? $"{channel.TrackCountText} · {channel.NewVideoText}"
            : $"{channel.TrackCountText} · no new videos";
        DetailSubtitleText.Text = channel.FollowerText.Length > 0
            ? $"{localStatus} · {channel.FollowerText}"
            : localStatus;
        DetailLibraryText.Text = channel.TrackCountText;
        DetailRatingText.Text = channel.RatingText;
        DetailActivityText.Text = channel.ActivityText;
        DetailTopTracksText.Text = channel.HasTopTracks ? channel.TopTracksText : "No local tracks yet";
        DetailFollowButton.Content = channel.IsFollowed ? "Following" : "+ Follow";
        DetailNotificationIcon.Text = channel.NotificationsEnabled ? "●" : "○";
        DetailNotificationButton.Opacity = channel.NotificationsEnabled ? 0.95 : 0.5;
        DetailAutoDownloadButton.Content = channel.AutoDownload ? "Auto-download on" : "Auto-download off";
        ChannelMaxDurationBox.Text = channel.MaxDurationMinutes?.ToString() ?? string.Empty;
        var effectiveLimit = channel.MaxDurationMinutes
                             ?? MusicLibraryService.Current.GetChannelMaxDownloadDurationMinutes();
        AutomationHintText.Text = channel.AutoDownload
            ? $"Future uploads only · up to {effectiveLimit} min"
            : $"Manual downloads · {effectiveLimit} min limit";
        ToolTip.SetTip(DetailNotificationButton, channel.NotificationsEnabled
            ? "Disable channel notifications"
            : "Enable channel notifications");
        UpdateRefreshPresentation();
    }

    private void OnBackToHubClicked(object? sender, RoutedEventArgs e)
    {
        ClearActivePreview();
        PreviewClosed?.Invoke();
        DetailView.IsVisible = false;
        if (_detailOpenedFromInbox)
        {
            HubView.IsVisible = false;
            InboxView.IsVisible = true;
            _detailOpenedFromInbox = false;
            RefreshInbox();
            RefreshInboxBadge();
        }
        else
        {
            InboxView.IsVisible = false;
            HubView.IsVisible = true;
            ApplyHubFilter();
        }
    }

    private void RefreshInboxBadge()
    {
        var unread = MusicLibraryService.Current.GetUnreadChannelNotificationCount();
        InboxBadge.IsVisible = unread > 0;
        InboxBadgeText.Text = unread > 99 ? "99+" : unread.ToString();
        if (InboxView.IsVisible)
            RefreshInbox();
    }

    private void RefreshInbox()
    {
        var notifications = MusicLibraryService.Current.GetChannelNotifications();
        InboxItems.ItemsSource = notifications;
        EmptyInboxText.IsVisible = notifications.Count == 0;
        var unread = notifications.Count(notification => !notification.IsRead);
        InboxSummaryText.Text = notifications.Count == 0
            ? "No pending channel updates"
            : $"{unread} unread · {notifications.Count} awaiting your review";
    }

    private void OnInboxClicked(object? sender, RoutedEventArgs e)
    {
        PreviewClosed?.Invoke();
        HubView.IsVisible = false;
        DetailView.IsVisible = false;
        InboxView.IsVisible = true;
        RefreshInbox();
    }

    private void OnBackFromInboxClicked(object? sender, RoutedEventArgs e)
    {
        InboxView.IsVisible = false;
        DetailView.IsVisible = false;
        HubView.IsVisible = true;
        ApplyHubFilter();
        RefreshInboxBadge();
    }

    private void OnOpenNotificationClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: ChannelNotification notification })
            return;

        MusicLibraryService.Current.MarkChannelNotificationRead(notification.Id);
        var channel = _hubChannels.FirstOrDefault(item => item.Id == notification.ChannelId);
        if (channel is null)
        {
            RefreshChannels();
            channel = _hubChannels.FirstOrDefault(item => item.Id == notification.ChannelId);
        }
        if (channel is null)
            return;

        _videoSearchByChannel[channel.Id] = notification.Title;
        _videoFiltersByChannel[channel.Id] = ChannelVideoFilter.All;
        OpenChannelDetail(channel, fromInbox: true);
        RefreshInboxBadge();
        e.Handled = true;
    }

    private void OnArchiveNotificationClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: ChannelNotification notification })
            return;
        MusicLibraryService.Current.ArchiveChannelNotification(notification.Id);
        RefreshInbox();
        RefreshInboxBadge();
        e.Handled = true;
    }

    private async void OnFollowChannelClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Control { DataContext: ChannelHubItem channel })
            return;
        var followed = !channel.IsFollowed;
        MusicLibraryService.Current.SetChannelFollowed(channel.Id, followed);
        RefreshChannels();
        ToastRequested?.Invoke(followed ? "Channel followed" : "Channel removed from Following");
        e.Handled = true;
        if (followed && string.IsNullOrWhiteSpace(channel.LastCheckedAt))
            await RefreshChannelDiscoveryAsync(channel, "Channel loaded");
    }

    private async void OnDetailFollowClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedHubChannel is not { } channel)
            return;
        var followed = !channel.IsFollowed;
        MusicLibraryService.Current.SetChannelFollowed(channel.Id, followed);
        RefreshChannels();
        ToastRequested?.Invoke(followed ? "Channel followed" : "Channel removed from Following");
        if (followed && string.IsNullOrWhiteSpace(channel.LastCheckedAt))
            await RefreshChannelDiscoveryAsync(channel, "Channel loaded");
    }

    private void OnDetailNotificationsClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedHubChannel is not { } channel)
            return;
        var enabled = !channel.NotificationsEnabled;
        MusicLibraryService.Current.SetChannelNotifications(channel.Id, enabled);
        RefreshChannels();
        ToastRequested?.Invoke(enabled ? "Channel notifications enabled" : "Channel notifications disabled");
    }

    private void OnDetailAutoDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedHubChannel is not { } channel)
            return;
        MusicLibraryService.Current.SetChannelAutoDownload(channel.Id, !channel.AutoDownload);
        RefreshChannels();
        ToastRequested?.Invoke(channel.AutoDownload ? "Auto-download disabled" : "Auto-download enabled");
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
                "Channel added",
                channelId: null);
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
        if (_selectedHubChannel is not { } channel || string.IsNullOrWhiteSpace(channel.SourceUrl))
            return;

        await RunRefreshAsync(
            progress => MusicLibraryService.Current.AddOrRefreshChannelAsync(
                channel.SourceUrl,
                progress,
                _refreshCts!.Token),
            "Channel refreshed",
            channel.Id);
    }

    private System.Threading.Tasks.Task RefreshChannelDiscoveryAsync(ChannelHubItem channel, string successText)
    {
        if (_refreshCts is not null || string.IsNullOrWhiteSpace(channel.SourceUrl))
            return System.Threading.Tasks.Task.CompletedTask;

        return RunRefreshAsync(
            progress => MusicLibraryService.Current.AddOrRefreshChannelAsync(
                channel.SourceUrl,
                progress,
                _refreshCts!.Token),
            successText,
            channel.Id);
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
        string successText,
        int? channelId)
    {
        if (_refreshCts is not null)
        {
            ToastRequested?.Invoke("Another channel refresh is already running");
            return;
        }

        var refreshCts = new CancellationTokenSource();
        _refreshCts = refreshCts;
        SetRefreshState(true, "Checking channel…", channelId);
        var progress = new Progress<string>(message => SetRefreshState(true, message, channelId));

        try
        {
            var result = await refresh(progress);
            if (!result.Success)
            {
                ToastRequested?.Invoke(result.Error ?? "Channel refresh failed");
                return;
            }

            RefreshChannels();
            if (channelId is int refreshedChannelId)
                ChannelMetadataService.Current.RequestChannel(refreshedChannelId, 20);
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
            if (ReferenceEquals(_refreshCts, refreshCts))
            {
                _refreshCts = null;
                SetRefreshState(false, channelId: channelId);
                UpdateDownloadSummary();
            }
            refreshCts.Dispose();
        }
    }

    private void SetRefreshState(bool active, string? message = null, int? channelId = null)
    {
        _refreshChannelId = active ? channelId : null;
        _refreshMessage = active ? message ?? "Refreshing…" : string.Empty;
        UpdateRefreshPresentation();
    }

    private void UpdateRefreshPresentation()
    {
        var active = _refreshCts is not null;
        var selectedChannelIsRefreshing = active
                                          && (_refreshChannelId is null
                                              || _refreshChannelId == _selectedChannelId);
        DetailRefreshButton.IsEnabled = !active;
        DetailRefreshButton.Opacity = active ? 0.35 : 1;
        ToolTip.SetTip(DetailRefreshButton, active
            ? selectedChannelIsRefreshing
                ? _refreshMessage
                : "Another channel refresh is currently running"
            : "Refresh channel");
        DetailRefreshStatusText.IsVisible = active && DetailView.IsVisible;
        DetailRefreshStatusText.Text = !active
            ? string.Empty
            : selectedChannelIsRefreshing
                ? _refreshMessage
                : "Another channel is refreshing…";
        StatusText.Text = active ? _refreshMessage : string.Empty;
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
        ApplyActivePreviewMarker();

        ApplyVideoView();
    }

    private void ApplyVideoView()
    {
        if (_selectedChannelId < 0)
            return;

        var newCount = _currentVideos.Count(video => !video.IsChecked);
        var readyCount = _currentVideos.Count(video => !video.IsChecked && video.TrackId is not null);
        var libraryCount = _currentVideos.Count(video => video.TrackId is not null);
        var issueCount = _currentVideos.Count(video =>
            video.DownloadStatus is ChannelDownloadStatus.Failed or ChannelDownloadStatus.Skipped
            || video.MetadataStatus == ChannelMetadataStatus.Failed);
        NewVideosFilterButton.Content = $"New {newCount}";
        ReadyVideosFilterButton.Content = $"Ready {readyCount}";
        LibraryVideosFilterButton.Content = $"In library {libraryCount}";
        IssueVideosFilterButton.Content = $"Issues {issueCount}";
        AllVideosFilterButton.Content = $"All {_currentVideos.Count}";

        var search = VideoSearchBox.Text?.Trim() ?? string.Empty;
        IEnumerable<ChannelVideoDisplay> videos = _currentVideos;
        if (search.Length > 0)
            videos = videos.Where(video => video.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        videos = _videoFilter switch
        {
            ChannelVideoFilter.New => videos.Where(video => !video.IsChecked),
            ChannelVideoFilter.Ready => videos.Where(video => !video.IsChecked && video.TrackId is not null),
            ChannelVideoFilter.InLibrary => videos.Where(video => video.TrackId is not null),
            ChannelVideoFilter.Issues => videos.Where(video =>
                video.DownloadStatus is ChannelDownloadStatus.Failed or ChannelDownloadStatus.Skipped
                || video.MetadataStatus == ChannelMetadataStatus.Failed),
            _ => videos
        };

        var visible = videos.ToList();

        _loadingVideos = true;
        try
        {
            VideoList.ItemsSource = visible;
        }
        finally
        {
            _loadingVideos = false;
        }
        UpdateEmptyVideoState(visible.Count, search);
        UpdateVideoSummary(visible.Count);
    }

    private void UpdateEmptyVideoState(int visibleCount, string search)
    {
        EmptyVideoPanel.IsVisible = visibleCount == 0;
        if (visibleCount > 0)
            return;

        if (search.Length > 0)
        {
            EmptyVideoTitleText.Text = "No matching videos";
            EmptyVideoDescriptionText.Text = "Try another title or clear the search.";
            return;
        }

        (EmptyVideoTitleText.Text, EmptyVideoDescriptionText.Text) = _videoFilter switch
        {
            ChannelVideoFilter.New => ("No new videos", "Everything discovered for this channel has been reviewed."),
            ChannelVideoFilter.Ready => ("Nothing ready yet", "Downloaded videos awaiting review will appear here."),
            ChannelVideoFilter.InLibrary => ("No local tracks", "Downloaded channel tracks will appear here."),
            ChannelVideoFilter.Issues => ("No issues", "Metadata and downloads are currently healthy."),
            _ => ("No videos discovered", "Refresh the channel to retrieve its uploads.")
        };
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
        if (sender is not Button button || button.DataContext is not ChannelVideoDisplay item || !item.CanDismiss)
            return;

        if (item.TrackId is null)
        {
            if (!MusicLibraryService.Current.DismissChannelVideo(item.Id))
            {
                ToastRequested?.Invoke("Video could not be removed from pending");
                return;
            }

            item.IsChecked = true;
            RefreshChannelSummaries();
            RefreshVideos();
            ToastRequested?.Invoke("Removed from pending");
            return;
        }

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
        _activePreviewTrackId = track.Id;
        ApplyActivePreviewMarker();
        PreviewRequested?.Invoke(track);
    }

    public void ClearActivePreview()
    {
        _activePreviewTrackId = null;
        ApplyActivePreviewMarker();
    }

    private void ApplyActivePreviewMarker()
    {
        foreach (var video in _currentVideos)
            video.IsActive = _activePreviewTrackId is int trackId && video.TrackId == trackId;
    }

    private void OnChannelMaxDurationLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || _selectedHubChannel is not { } channel)
            return;

        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            MusicLibraryService.Current.SetChannelMaxDownloadDuration(channel.Id, null);
            RefreshChannels();
            UpdateDownloadSummary();
            ToastRequested?.Invoke("Channel uses the global download limit");
            return;
        }
        if (!int.TryParse(textBox.Text, out var minutes))
        {
            textBox.Text = channel.MaxDurationMinutes?.ToString() ?? string.Empty;
            ToastRequested?.Invoke("Enter a duration between 1 and 180 minutes");
            return;
        }

        minutes = Math.Clamp(
            minutes,
            AppSettingsStore.ChannelDownloadMinDurationMinutes,
            AppSettingsStore.ChannelDownloadMaxDurationMinutes);
        MusicLibraryService.Current.SetChannelMaxDownloadDuration(channel.Id, minutes);
        textBox.Text = minutes.ToString();
        RefreshChannels();
        UpdateDownloadSummary();
        ToastRequested?.Invoke($"Channel download limit set to {minutes} min");
    }

    private void OnVideoSearchChanged(object? sender, TextChangedEventArgs e)
    {
        if (_selectedChannelId < 0)
            return;
        _videoSearchByChannel[_selectedChannelId] = VideoSearchBox.Text ?? string.Empty;
        ApplyVideoView();
    }

    private void OnNewVideosFilterClicked(object? sender, RoutedEventArgs e) =>
        SetVideoFilter(ChannelVideoFilter.New);

    private void OnReadyVideosFilterClicked(object? sender, RoutedEventArgs e) =>
        SetVideoFilter(ChannelVideoFilter.Ready);

    private void OnLibraryVideosFilterClicked(object? sender, RoutedEventArgs e) =>
        SetVideoFilter(ChannelVideoFilter.InLibrary);

    private void OnIssueVideosFilterClicked(object? sender, RoutedEventArgs e) =>
        SetVideoFilter(ChannelVideoFilter.Issues);

    private void OnAllVideosFilterClicked(object? sender, RoutedEventArgs e) =>
        SetVideoFilter(ChannelVideoFilter.All);

    private void SetVideoFilter(ChannelVideoFilter filter, bool refresh = true)
    {
        _videoFilter = filter;
        if (_selectedChannelId >= 0)
            _videoFiltersByChannel[_selectedChannelId] = filter;

        var buttons = new Dictionary<ChannelVideoFilter, Button>
        {
            [ChannelVideoFilter.New] = NewVideosFilterButton,
            [ChannelVideoFilter.Ready] = ReadyVideosFilterButton,
            [ChannelVideoFilter.InLibrary] = LibraryVideosFilterButton,
            [ChannelVideoFilter.Issues] = IssueVideosFilterButton,
            [ChannelVideoFilter.All] = AllVideosFilterButton
        };
        foreach (var pair in buttons)
        {
            var selected = pair.Key == filter;
            pair.Value.Background = selected
                ? new SolidColorBrush(Color.Parse("#293E6591"))
                : Brushes.Transparent;
            pair.Value.BorderBrush = selected
                ? new SolidColorBrush(Color.Parse("#6B83A9CA"))
                : new SolidColorBrush(Color.Parse("#26FFFFFF"));
            pair.Value.Foreground = ThemeResources.Brush(selected
                ? "Theme.Brush.TextPrimary"
                : "Theme.Brush.TextSecondary");
        }

        if (refresh)
            ApplyVideoView();
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
        RefreshChannels();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        _refreshCts?.Cancel();
        ClearActivePreview();
        CloseRequested?.Invoke();
    }

    private void UpdateStatus()
    {
        ApplyHubFilter();
    }

    private void RefreshChannelStates()
    {
        if (!IsVisible)
            return;
        RefreshChannels();
    }

    private void UpdateVideoSummary(int? visibleCount = null)
    {
        if (_selectedChannelId < 0)
        {
            VideoSummaryText.Text = "No channel selected";
            return;
        }

        var uncheckedCount = _currentVideos.Count(video => !video.IsChecked);
        var ready = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Ready);
        var queued = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Queued);
        var downloading = _currentVideos.Count(video => video.DownloadStatus == ChannelDownloadStatus.Downloading);
        var enriched = _currentVideos.Count(video => video.MetadataStatus == ChannelMetadataStatus.Ready);
        var active = queued + downloading;
        var issues = _currentVideos.Count(video =>
            video.DownloadStatus is ChannelDownloadStatus.Failed or ChannelDownloadStatus.Skipped
            || video.MetadataStatus == ChannelMetadataStatus.Failed);
        var shown = visibleCount ?? _currentVideos.Count;
        VideoSummaryText.Text = $"{shown} shown · {_currentVideos.Count} total · {enriched} enriched · " +
                                $"{uncheckedCount} new · {ready} downloaded · {active} active · {issues} issues";
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
    private bool _isActive;
    private bool _canDismiss;
    private string _dismissToolTip = "Skip (keep audio)";
    private IBrush _borderBrush = Brushes.Transparent;
    private Thickness _borderThickness;

    public ChannelVideoDisplay(ChannelVideo video)
    {
        Id = video.Id;
        CanonicalUrl = video.CanonicalUrl;
        Title = video.Title;
        DurationText = video.DurationSeconds is int seconds ? FormatDuration(seconds) : "";
        UploadedText = FormatUploadDate(video.UploadedAt);
        MetadataSummaryText = BuildMetadataSummary(video, UploadedText);
        MetadataErrorDetails = TrimErrorDetails(video.MetadataError);
        MetadataErrorSummary = ErrorSummary(MetadataErrorDetails);
        MetadataStatus = video.MetadataStatus;
        _trackId = video.TrackId;
        SetDownloadState(video.DownloadStatus, video.DownloadError);
        SetChecked(video.IsChecked);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; }
    public string CanonicalUrl { get; }
    public string Title { get; }
    public string UploadedText { get; }
    public bool HasUploadDate => UploadedText.Length > 0;
    public string MetadataSummaryText { get; }
    public bool HasMetadataSummary => MetadataSummaryText.Length > 0;
    public string MetadataErrorSummary { get; }
    public string MetadataErrorDetails { get; }
    public bool HasMetadataError => MetadataErrorSummary.Length > 0;
    public ChannelMetadataStatus MetadataStatus { get; }
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
    public bool CanDismiss
    {
        get => _canDismiss;
        private set => SetField(ref _canDismiss, value);
    }
    public string DismissToolTip
    {
        get => _dismissToolTip;
        private set => SetField(ref _dismissToolTip, value);
    }
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
                return;
            SetField(ref _isActive, value);
            UpdateVisualState();
        }
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
    public IBrush BorderBrush
    {
        get => _borderBrush;
        private set => SetField(ref _borderBrush, value);
    }
    public Thickness BorderThickness
    {
        get => _borderThickness;
        private set => SetField(ref _borderThickness, value);
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
        CheckBackground = ThemeResources.Brush(value ? "Theme.Brush.Success" : "Theme.Brush.Surface");
        CheckBorder = ThemeResources.Brush(value ? "Theme.Brush.Accent" : "Theme.Brush.Border");
        CheckOpacity = value ? 1 : 0.58;
        TextDecorations = value ? Avalonia.Media.TextDecorations.Strikethrough : null;
        if (value && TrackId is not null)
            StatusText = "In library";
        CanReview = !value && TrackId is not null;
        UpdateActions();
        UpdateVisualState();
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
        UpdateActions();
    }

    private void UpdateActions()
    {
        CanDismiss = !IsChecked && (TrackId is not null || DownloadStatus == ChannelDownloadStatus.Skipped);
        DismissToolTip = TrackId is null ? "Remove from pending" : "Skip (keep audio)";
        ActionOpacity = !IsChecked && (TrackId is not null || CanDismiss) ? 0.78 : 0.3;
    }

    private void UpdateVisualState()
    {
        Opacity = IsActive ? 1 : IsChecked ? 0.45 : 1;
        Background = IsActive
            ? ThemeResources.Brush("Theme.Brush.SurfaceSelected")
            : IsChecked
                ? new SolidColorBrush(Color.Parse("#22192027"))
                : new SolidColorBrush(Color.Parse("#10203422"));
        BorderBrush = IsActive ? ThemeResources.Brush("Theme.Brush.Accent") : Brushes.Transparent;
        BorderThickness = IsActive ? new Thickness(1) : new Thickness(0);
    }

    private void SetDownloadError(string? error)
    {
        DownloadErrorDetails = TrimErrorDetails(error);
        DownloadErrorSummary = ErrorSummary(DownloadErrorDetails);
        HasDownloadError = DownloadErrorSummary.Length > 0;
    }

    private static string TrimErrorDetails(string? error)
    {
        var details = error?.Trim() ?? string.Empty;
        return details.Length > 4000 ? details[..4000] + "…" : details;
    }

    private static string ErrorSummary(string details)
    {
        var lines = details.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var summary = lines.LastOrDefault(line => line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
                      ?? lines.LastOrDefault()
                      ?? string.Empty;
        return summary.Length > 260 ? summary[..260] + "…" : summary;
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

    private static string FormatUploadDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        if (value.Length == 8
            && DateTime.TryParseExact(value, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var compactDate))
            return compactDate.ToString("dd MMM yyyy");
        return DateTime.TryParse(value, out var parsed)
            ? parsed.ToString("dd MMM yyyy")
            : value;
    }

    private static string BuildMetadataSummary(ChannelVideo video, string uploadedText)
    {
        var parts = new List<string>();
        if (uploadedText.Length > 0)
            parts.Add(uploadedText);
        if (video.ViewCount is long views)
            parts.Add($"{FormatCompactNumber(views)} views");
        if (video.LikeCount is long likes)
            parts.Add($"{FormatCompactNumber(likes)} likes");
        if (video.MetadataStatus is ChannelMetadataStatus.Queued or ChannelMetadataStatus.Loading)
            parts.Add("loading metadata…");
        return string.Join(" · ", parts);
    }

    private static string FormatCompactNumber(long value) => value switch
    {
        >= 1_000_000_000 => $"{value / 1_000_000_000d:0.#}B",
        >= 1_000_000 => $"{value / 1_000_000d:0.#}M",
        >= 1_000 => $"{value / 1_000d:0.#}K",
        _ => value.ToString()
    };

}
