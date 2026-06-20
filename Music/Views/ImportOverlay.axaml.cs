using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class ImportOverlay : UserControl
{
    private string? _canonicalUrl;
    private bool _updatingUrl;
    private bool _isDownloading;

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
        BusyPanel.IsVisible = false;
        ImportForm.IsEnabled = true;
        _isDownloading = false;
        IsVisible = true;
        UrlBox.Focus();
    }

    private void ValidateUrl()
    {
        if (_updatingUrl) return;
        var rawUrl = UrlBox.Text?.Trim() ?? string.Empty;
        var videoId = YouTubeUrlNormalizer.ExtractVideoId(rawUrl);
        if (videoId is null)
        {
            _canonicalUrl = null;
            StatusText.Text = rawUrl.Length == 0 ? string.Empty : "Enter a valid YouTube URL.";
            return;
        }

        var canonicalUrl = YouTubeUrlNormalizer.GetCanonicalUrl(videoId);
        if (MusicLibraryService.Current.TrackExistsByCanonicalUrl(canonicalUrl))
        {
            _canonicalUrl = null;
            StatusText.Text = "This track already exists in the library.";
            return;
        }

        _canonicalUrl = canonicalUrl;
        StatusText.Text = "";
        if (!string.Equals(rawUrl, canonicalUrl, StringComparison.Ordinal))
        {
            _updatingUrl = true;
            UrlBox.Text = canonicalUrl;
            UrlBox.CaretIndex = canonicalUrl.Length;
            _updatingUrl = false;
        }

        _ = DownloadAsync();
    }

    private async System.Threading.Tasks.Task DownloadAsync()
    {
        if (_canonicalUrl is null || _isDownloading) return;
        _isDownloading = true;
        ImportForm.IsEnabled = false;
        BusyPanel.IsVisible = true;
        var progress = new Progress<string>(message => BusyText.Text = message);
        var result = await MusicLibraryService.Current.ImportFromYouTubeAsync(_canonicalUrl, progress);
        BusyPanel.IsVisible = false;
        ImportForm.IsEnabled = true;
        if (!result.Success || result.Track is null)
        {
            StatusText.Text = result.Error ?? "Import failed.";
            _isDownloading = false;
            return;
        }

        IsVisible = false;
        TrackImported?.Invoke(result.Track);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;

    private void OnBackdropPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_isDownloading)
            IsVisible = false;
    }
}
