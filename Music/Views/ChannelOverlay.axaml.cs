using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ChannelOverlay : UserControl
{
    private CancellationTokenSource? _refreshCts;
    private List<ChannelSubscription> _channels = [];
    private int _selectedChannelId = -1;
    private bool _loadingVideos;

    public event Action? CloseRequested;
    public event Action<string>? ToastRequested;

    public ChannelOverlay()
    {
        InitializeComponent();
    }

    public void Open()
    {
        IsVisible = true;
        RefreshChannels();
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

    private async void OnAddClicked(object? sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            ToastRequested?.Invoke("Channel URL is required");
            return;
        }

        await RunRefreshAsync(
            progress => MusicLibraryService.Current.AddOrRefreshChannelAsync(url, progress, _refreshCts!.Token),
            "Channel added");
        UrlBox.Text = string.Empty;
    }

    private async void OnRefreshClicked(object? sender, RoutedEventArgs e)
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        StatusText.Text = "Refreshing channels...";
        try
        {
            var added = await MusicLibraryService.Current.RefreshSubscribedChannelsAsync(_refreshCts.Token);
            RefreshChannels();
            ToastRequested?.Invoke(added > 0 ? $"{added} new channel videos found" : "Channels are up to date");
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

    private async void OnRefreshSelectedClicked(object? sender, RoutedEventArgs e)
    {
        if (ChannelList.SelectedItem is not ChannelSubscription channel)
            return;

        await RunRefreshAsync(
            progress => MusicLibraryService.Current.RefreshChannelAsync(channel, progress, _refreshCts!.Token),
            "Channel refreshed");
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

        var videos = MusicLibraryService.Current.GetChannelVideos(_selectedChannelId)
            .Select(video => new ChannelVideoDisplay(video))
            .ToList();
        _loadingVideos = true;
        try
        {
            VideoList.ItemsSource = videos;
        }
        finally
        {
            _loadingVideos = false;
        }
        var uncheckedCount = videos.Count(video => !video.IsChecked);
        VideoSummaryText.Text = $"{videos.Count} videos · {uncheckedCount} unchecked";
    }

    private void OnVideoCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not ChannelVideoDisplay item)
            return;
        if (_loadingVideos)
            return;

        var isChecked = checkBox.IsChecked == true;
        MusicLibraryService.Current.SetChannelVideoChecked(item.Id, isChecked);
        item.IsChecked = isChecked;
        RefreshChannelSummaries();
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
    private IBrush _background = Brushes.Transparent;
    private TextDecorationCollection? _textDecorations;

    public ChannelVideoDisplay(ChannelVideo video)
    {
        Id = video.Id;
        CanonicalUrl = video.CanonicalUrl;
        Title = video.Title;
        DurationText = video.DurationSeconds is int seconds ? FormatDuration(seconds) : "";
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
        TextDecorations = value ? Avalonia.Media.TextDecorations.Strikethrough : null;
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
