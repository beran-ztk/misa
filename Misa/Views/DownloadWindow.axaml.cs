using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Misa.Models;

namespace Misa.Views;

public partial class DownloadWindow : Window
{
    private const string ToolsDir = @"D:\media\tools";
    private const string MusicDir = @"D:\media\music";

    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    public DownloadWindow()
    {
        InitializeComponent();
        UrlBox.TextChanged += (_, _) => UpdateDownloadButton();
        GenreBox.SelectionChanged += (_, _) => UpdateDownloadButton();
        RatingBox.SelectionChanged += (_, _) => UpdateDownloadButton();
        LoadLookups();
    }

    private void LoadLookups()
    {
        _genres = Db.GetGenres();
        _ratings = Db.GetRatings();
        _styles = Db.GetStyles();

        GenreBox.ItemsSource = new[] { "(Select genre)" }.Concat(_genres.Select(g => g.Name)).ToList();
        GenreBox.SelectedIndex = 0;
        RatingBox.ItemsSource = new[] { "(Select rating)" }.Concat(_ratings.Select(r => r.Name)).ToList();
        RatingBox.SelectedIndex = 0;
        StylesBox.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void UpdateDownloadButton()
    {
        DownloadBtn.IsEnabled = !string.IsNullOrWhiteSpace(UrlBox.Text)
                               && GenreBox.SelectedIndex > 0
                               && RatingBox.SelectedIndex > 0;
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
        CloseBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var (success, errorOutput) = await RunYtDlp(canonicalUrl);

        if (!success)
        {
            StatusText.Text = $"Failed:\n{errorOutput}";
            UpdateDownloadButton();
            CloseBtn.IsEnabled = true;
            return;
        }

        var filePath = FindDownloadedFile(videoId);
        if (filePath == null)
        {
            StatusText.Text = "Download finished but file not found.";
            UpdateDownloadButton();
            CloseBtn.IsEnabled = true;
            return;
        }

        var fileName = Path.GetFileName(filePath);
        var title = string.IsNullOrWhiteSpace(TitleBox.Text)
            ? TitleFromFileName(fileName)
            : TitleBox.Text.Trim();

        var genreId = _genres[GenreBox.SelectedIndex - 1].Id;
        var ratingId = _ratings[RatingBox.SelectedIndex - 1].Id;
        var styleIds = StylesBox.SelectedItems?
            .Cast<string>()
            .Select(name => _styles.First(s => s.Name == name).Id)
            .ToList() ?? [];

        var duration = await GetDurationAsync(filePath);
        Db.InsertTrack(canonicalUrl, title, fileName, genreId, ratingId, styleIds, duration);

        Close(true);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(false);

    private void OnAddGenreClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewGenreBox.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;
        Db.InsertGenre(name);
        NewGenreBox.Text = "";
        var prevGenre = GenreBox.SelectedIndex;
        LoadLookups();
        GenreBox.SelectedIndex = prevGenre;
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

    private static async Task<int?> GetDurationAsync(string filePath)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(ToolsDir, "ffprobe.exe"),
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = Process.Start(psi)!;
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (double.TryParse(output.Trim(), System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var seconds))
                return (int)seconds;
        }
        catch { }
        return null;
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
