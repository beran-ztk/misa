using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ImportOverlay : UserControl
{
    private string? _canonicalUrl;
    private bool _updatingUrl;

    public event Action<MusicTrack>? TrackImported;

    public ImportOverlay()
    {
        InitializeComponent();
        UrlBox.TextChanged += (_, _) => ValidateUrl();
    }

    public void Open()
    {
        _canonicalUrl = null;
        UrlBox.Text = string.Empty;
        StatusText.Text = string.Empty;
        SourcePanel.IsVisible = true;
        YouTubePanel.IsVisible = false;
        BusyLayer.IsVisible = false;
        IsVisible = true;
    }

    private void OnYouTubeSelected(object? sender, RoutedEventArgs e)
    {
        SourcePanel.IsVisible = false;
        YouTubePanel.IsVisible = true;
        UrlBox.Focus();
    }

    private void OnBackClicked(object? sender, RoutedEventArgs e)
    {
        SourcePanel.IsVisible = true;
        YouTubePanel.IsVisible = false;
    }

    private void ValidateUrl()
    {
        if (_updatingUrl) return;
        var rawUrl = UrlBox.Text?.Trim() ?? string.Empty;
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(rawUrl);
        if (videoId is null)
        {
            _canonicalUrl = null;
            DownloadButton.IsEnabled = false;
            StatusText.Text = rawUrl.Length == 0 ? string.Empty : "Enter a valid YouTube URL.";
            return;
        }

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);
        if (MusicLibraryService.Current.TrackExistsByCanonicalUrl(canonicalUrl))
        {
            _canonicalUrl = null;
            DownloadButton.IsEnabled = false;
            StatusText.Text = "This track already exists in the library.";
            return;
        }

        _canonicalUrl = canonicalUrl;
        DownloadButton.IsEnabled = true;
        StatusText.Text = "Ready to download and analyze.";
        if (!string.Equals(rawUrl, canonicalUrl, StringComparison.Ordinal))
        {
            _updatingUrl = true;
            UrlBox.Text = canonicalUrl;
            UrlBox.CaretIndex = canonicalUrl.Length;
            _updatingUrl = false;
        }
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        if (_canonicalUrl is null) return;
        BusyLayer.IsVisible = true;
        var progress = new Progress<string>(message => BusyText.Text = message);
        var result = await MusicLibraryService.Current.ImportFromYouTubeAsync(_canonicalUrl, progress);
        BusyLayer.IsVisible = false;
        if (!result.Success || result.Track is null)
        {
            StatusText.Text = result.Error ?? "Import failed.";
            return;
        }

        IsVisible = false;
        TrackImported?.Invoke(result.Track);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;
}
