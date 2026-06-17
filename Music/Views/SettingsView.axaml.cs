using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class SettingsView : UserControl
{
    public Action? PrepareForReset;
    public Action? OnResetComplete;
    public Action? OnMetadataChanged;

    private List<Genre> _genres = [];
    private List<Style> _styles = [];
    private List<Rating> _ratings = [];

    public SettingsView()
    {
        InitializeComponent();
        LoadAll();
    }

    private void LoadAll()
    {
        LoadMusicSettings();
        LoadGenres();
        LoadStyles();
        LoadRatings();
    }

    // --- Music Settings ---

    private void LoadMusicSettings()
    {
        var s = MusicLibraryService.Current.GetSettings();
        MusicDirBox.Text = s.MusicDirectory;
        ToolsDirBox.Text = s.ToolsDirectory;
        UseNodeJsCheckBox.IsChecked = s.UseNodeJsRuntime;
        UseFirefoxCheckBox.IsChecked = s.UseFirefoxCookies;
        UseRemoteEjsCheckBox.IsChecked = s.UseRemoteEjsComponents;
        CrossfadeDurationBox.Value = s.CrossfadeDurationSeconds;
        ManualFadeDurationBox.Value = s.ManualSwitchFadeDurationSeconds;
        ShowUpcomingBarCheckBox.IsChecked = s.ShowUpcomingTrackBar;
    }

    private async void OnBrowseMusicDirClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Music Directory",
            AllowMultiple = false,
        });
        if (folders.Count > 0)
            MusicDirBox.Text = folders[0].Path.LocalPath;
    }

    private async void OnBrowseToolsDirClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;
        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Tools Directory",
            AllowMultiple = false,
        });
        if (folders.Count > 0)
            ToolsDirBox.Text = folders[0].Path.LocalPath;
    }

    private void OnSaveSettingsClicked(object? sender, RoutedEventArgs e)
    {
        // Start from the live settings object to preserve player state (volume, etc.).
        var settings = MusicLibraryService.Current.GetSettings();
        settings.MusicDirectory = MusicDirBox.Text?.Trim() ?? "";
        settings.ToolsDirectory = ToolsDirBox.Text?.Trim() ?? "";
        settings.UseNodeJsRuntime = UseNodeJsCheckBox.IsChecked == true;
        settings.UseFirefoxCookies = UseFirefoxCheckBox.IsChecked == true;
        settings.UseRemoteEjsComponents = UseRemoteEjsCheckBox.IsChecked == true;
        if (CrossfadeDurationBox.Value.HasValue)
            settings.CrossfadeDurationSeconds = Math.Clamp((int)CrossfadeDurationBox.Value.Value, 0, 30);
        if (ManualFadeDurationBox.Value.HasValue)
            settings.ManualSwitchFadeDurationSeconds = Math.Clamp((int)ManualFadeDurationBox.Value.Value, 0, 10);
        settings.ShowUpcomingTrackBar = ShowUpcomingBarCheckBox.IsChecked == true;
        PrepareForReset?.Invoke();
        MusicLibraryService.Current.SaveSettings(settings);
        SettingsStatus.Text = "Settings saved.";
        OnResetComplete?.Invoke();
    }

    private async void OnTestToolsClicked(object? sender, RoutedEventArgs e)
    {
        var toolsDir = ToolsDirBox.Text?.Trim() ?? "";
        var sb = new StringBuilder();

        foreach (var tool in new[] { "yt-dlp.exe", "ffmpeg.exe", "ffprobe.exe" })
        {
            var path = Path.Combine(toolsDir, tool);
            sb.AppendLine(File.Exists(path) ? $"✓ {tool}" : $"✗ {tool} — not found");
        }

        if (UseNodeJsCheckBox.IsChecked == true)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var process = Process.Start(psi)!;
                var version = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                sb.AppendLine(process.ExitCode == 0
                    ? $"✓ node {version.Trim()}"
                    : "✗ node — not available");
            }
            catch
            {
                sb.AppendLine("✗ node — not found in PATH");
            }
        }

        SettingsStatus.Text = sb.ToString().TrimEnd();
    }

    // --- Genres ---

    private void LoadGenres()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        GenreList.ItemsSource = _genres.Select(g => g.Name).ToList();
    }

    private void OnGenreSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx >= 0 && idx < _genres.Count)
            GenreInput.Text = _genres[idx].Name;
    }

    private void OnAddGenreClicked(object? sender, RoutedEventArgs e)
    {
        var name = GenreInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { GenreStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddGenre(name);
        GenreInput.Text = "";
        GenreStatus.Text = "Added.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameGenreClicked(object? sender, RoutedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx < 0) { GenreStatus.Text = "Select a genre first."; return; }
        var name = GenreInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { GenreStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameGenre(_genres[idx].Id, name);
        GenreStatus.Text = "Renamed.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteGenreClicked(object? sender, RoutedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx < 0) { GenreStatus.Text = "Select a genre first."; return; }
        var result = MusicLibraryService.Current.TryDeleteGenre(_genres[idx].Id);
        if (!result.Success) { GenreStatus.Text = result.Error; return; }
        GenreStatus.Text = "Deleted.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    // --- Styles ---

    private void LoadStyles()
    {
        _styles = MusicLibraryService.Current.GetStyles();
        StyleList.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void OnStyleSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx >= 0 && idx < _styles.Count)
            StyleInput.Text = _styles[idx].Name;
    }

    private void OnAddStyleClicked(object? sender, RoutedEventArgs e)
    {
        var name = StyleInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { StyleStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddStyle(name);
        StyleInput.Text = "";
        StyleStatus.Text = "Added.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameStyleClicked(object? sender, RoutedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx < 0) { StyleStatus.Text = "Select a style first."; return; }
        var name = StyleInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { StyleStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameStyle(_styles[idx].Id, name);
        StyleStatus.Text = "Renamed.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteStyleClicked(object? sender, RoutedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx < 0) { StyleStatus.Text = "Select a style first."; return; }
        var result = MusicLibraryService.Current.TryDeleteStyle(_styles[idx].Id);
        if (!result.Success) { StyleStatus.Text = result.Error; return; }
        StyleStatus.Text = "Deleted.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    // --- Ratings ---

    private void LoadRatings()
    {
        _ratings = MusicLibraryService.Current.GetRatings();
        RatingList.ItemsSource = _ratings.Select(r => r.Name).ToList();
    }

    private void OnRatingSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx >= 0 && idx < _ratings.Count)
            RatingInput.Text = _ratings[idx].Name;
    }

    private void OnAddRatingClicked(object? sender, RoutedEventArgs e)
    {
        var name = RatingInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { RatingStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddRating(name);
        RatingInput.Text = "";
        RatingStatus.Text = "Added.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameRatingClicked(object? sender, RoutedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx < 0) { RatingStatus.Text = "Select a rating first."; return; }
        var name = RatingInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { RatingStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameRating(_ratings[idx].Id, name);
        RatingStatus.Text = "Renamed.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteRatingClicked(object? sender, RoutedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx < 0) { RatingStatus.Text = "Select a rating first."; return; }
        var result = MusicLibraryService.Current.TryDeleteRating(_ratings[idx].Id);
        if (!result.Success) { RatingStatus.Text = result.Error; return; }
        RatingStatus.Text = "Deleted.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }
}
