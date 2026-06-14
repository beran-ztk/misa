using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Misa.Models;
using NAudio.Wave;

namespace Misa.Views;

public partial class MainWindow : Window
{
    private const string ToolsDir = @"D:\media\tools";
    private const string MusicDir = @"D:\media\music";

    private IWavePlayer? _player;
    private WaveStream? _audioStream;
    private List<MusicTrack> _tracks = [];
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            Db.Initialize();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Database error: {ex.Message}";
            return;
        }
        LoadLookups();
        RefreshTrackList();
    }

    private void LoadLookups()
    {
        _genres = Db.GetGenres();
        _ratings = Db.GetRatings();
        _styles = Db.GetStyles();

        GenreBox.ItemsSource = new[] { "" }.Concat(_genres.Select(g => g.Name)).ToList();
        RatingBox.ItemsSource = new[] { "" }.Concat(_ratings.Select(r => r.Name)).ToList();
        StylesBox.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void RefreshTrackList()
    {
        _tracks = Db.GetAllTracks();
        FileList.ItemsSource = _tracks.Select(t => t.Title).ToList();
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        var rawUrl = UrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(rawUrl)) return;

        var videoId = ExtractVideoId(rawUrl);
        if (videoId == null)
        {
            StatusText.Text = "Could not parse YouTube URL.";
            return;
        }

        var canonicalUrl = $"https://www.youtube.com/watch?v={videoId}";

        if (Db.TrackExists(canonicalUrl))
        {
            StatusText.Text = "Track already exists.";
            return;
        }

        DownloadBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var (success, errorOutput) = await RunYtDlp(canonicalUrl);

        if (!success)
        {
            StatusText.Text = $"Failed:\n{errorOutput}";
            DownloadBtn.IsEnabled = true;
            return;
        }

        var filePath = FindDownloadedFile(videoId);
        if (filePath == null)
        {
            StatusText.Text = "Download finished but file not found.";
            DownloadBtn.IsEnabled = true;
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var title = string.IsNullOrWhiteSpace(TitleBox.Text)
            ? TitleFromFileName(fileName)
            : TitleBox.Text.Trim();

        var genreId = GenreBox.SelectedIndex > 0 ? _genres[GenreBox.SelectedIndex - 1].Id : (int?)null;
        var ratingId = RatingBox.SelectedIndex > 0 ? _ratings[RatingBox.SelectedIndex - 1].Id : (int?)null;
        var styleIds = StylesBox.SelectedItems?
            .Cast<string>()
            .Select(name => _styles.First(s => s.Name == name).Id)
            .ToList() ?? [];

        Db.InsertTrack(canonicalUrl, title, fileName, genreId, ratingId, styleIds);

        RefreshTrackList();
        StatusText.Text = "Finished.";
        UrlBox.Text = "";
        TitleBox.Text = "";
        GenreBox.SelectedIndex = 0;
        RatingBox.SelectedIndex = 0;
        StylesBox.UnselectAll();
        DownloadBtn.IsEnabled = true;
    }

    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _tracks.Count) return;

        //StopPlayback();

        try
        {
            _audioStream = new MediaFoundationReader(Path.Combine(MusicDir, _tracks[idx].FileName));
            _player = new WaveOutEvent();
            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(_audioStream);
            _player.Play();
            NowPlayingText.Text = _tracks[idx].Title;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
        }
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        StopPlayback();
        NowPlayingText.Text = "";
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => NowPlayingText.Text = "");
    }

    private void StopPlayback()
    {
        if (_player != null)
        {
            _player.PlaybackStopped -= OnPlaybackStopped;
            _player.Stop();
            _player.Dispose();
        }
        _audioStream?.Dispose();
        _player = null;
        _audioStream = null;
    }

    private void OnAddGenreClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewGenreBox.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        Db.InsertGenre(name);
        NewGenreBox.Text = "";
        var prev = GenreBox.SelectedItem;
        LoadLookups();
        GenreBox.SelectedItem = prev;
    }

    private void OnAddStyleClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewStyleBox.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        Db.InsertStyle(name);
        NewStyleBox.Text = "";
        LoadLookups();
    }

    private static async Task<(bool success, string errorOutput)> RunYtDlp(string url)
    {
        var outputTemplate = Path.Combine(MusicDir, @"%(title)s [%(id)s].%(ext)s");

        var psi = new ProcessStartInfo
        {
            FileName = Path.Combine(ToolsDir, "yt-dlp.exe"),
            // Do not manually add tools as parameter
            Arguments = $"--js-runtimes node --cookies-from-browser firefox --no-playlist -x --audio-format m4a -o \"{outputTemplate}\" \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var stderr = await stderrTask;
        await stdoutTask;

        return (process.ExitCode == 0, stderr);
    }

    private static string? FindDownloadedFile(string videoId)
    {
        return Directory.GetFiles(MusicDir, "*.m4a")
                        .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).EndsWith($"[{videoId}]"));
    }

    private static string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var bracket = name.LastIndexOf('[');
        return bracket > 0 ? name[..bracket].Trim() : name;
    }

    private static string? ExtractVideoId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        if (uri.Host is "youtu.be" or "www.youtu.be")
            return uri.AbsolutePath.TrimStart('/').Split('?')[0];

        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var eq = part.IndexOf('=');
            if (eq > 0 && part[..eq] == "v")
                return Uri.UnescapeDataString(part[(eq + 1)..]);
        }

        return null;
    }
}
