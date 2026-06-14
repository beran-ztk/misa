using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Misa.Models;
using NAudio.Wave;

namespace Misa.Views;

public partial class MusicView : UserControl
{
    private const string MusicDir = @"D:\media\music";

    private IWavePlayer? _player;
    private WaveStream? _audioStream;
    private int _playingTrackId = -1;

    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];
    private List<TrackDisplayItem> _allItems = [];
    private List<TrackDisplayItem> _filteredItems = [];

    public MusicView()
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
        GenreFilter.ItemsSource = _genres.Select(g => g.Name).ToList();
        RatingFilter.ItemsSource = _ratings.Select(r => r.Name).ToList();
        StyleFilter.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void RefreshTrackList()
    {
        var tracks = Db.GetAllTracks();
        var allStyleIds = Db.GetAllMusicStyleIds();
        var genreMap = _genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = _ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = _styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var styleIds = allStyleIds.GetValueOrDefault(t.Id, []);
            var styleNames = styleIds
                .Select(id => styleMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0)
                .Order();

            var parts = new List<string>
            {
                genreMap.GetValueOrDefault(t.GenreId, "?"),
                ratingMap.GetValueOrDefault(t.RatingId, "?"),
            };
            if (t.DurationSeconds.HasValue)
                parts.Add(FormatDuration(t.DurationSeconds.Value));
            var styleStr = string.Join(", ", styleNames);
            if (styleStr.Length > 0)
                parts.Add(styleStr);

            return new TrackDisplayItem(t, string.Join(" · ", parts), styleIds);
        }).ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selGenreIds = SelectedIds(GenreFilter, _genres, g => g.Name, g => g.Id);
        var selRatingIds = SelectedIds(RatingFilter, _ratings, r => r.Name, r => r.Id);
        var selStyleIds = SelectedIds(StyleFilter, _styles, s => s.Name, s => s.Id);

        _filteredItems = _allItems.Where(item =>
            (selGenreIds.Count == 0 || selGenreIds.Contains(item.GenreId)) &&
            (selRatingIds.Count == 0 || selRatingIds.Contains(item.RatingId)) &&
            (selStyleIds.Count == 0 || item.StyleIds.Any(id => selStyleIds.Contains(id)))
        ).ToList();

        FileList.ItemsSource = _filteredItems;
    }

    private static HashSet<int> SelectedIds<T>(ListBox filter, List<T> source,
        Func<T, string> nameOf, Func<T, int> idOf)
    {
        var selected = filter.SelectedItems?.Cast<string>().ToHashSet() ?? [];
        if (selected.Count == 0) return [];
        return source.Where(item => selected.Contains(nameOf(item))).Select(idOf).ToHashSet();
    }

    private void OnFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    public void Refresh()
    {
        NowPlayingText.Text = "";
        LoadLookups();
        RefreshTrackList();
    }

    private async void OnAddTrackClicked(object? sender, RoutedEventArgs e)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;
        var downloaded = await new DownloadWindow().ShowDialog<bool>(owner);
        if (downloaded) RefreshTrackList();
    }

    private async void OnContextEditClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;

        var saved = await new EditTrackWindow(_filteredItems[idx].Track).ShowDialog<bool>(owner);
        if (saved) RefreshTrackList();
    }

    private async void OnContextDeleteClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;

        var track = _filteredItems[idx].Track;
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null) return;

        var confirmed = await new ConfirmDialog(
            $"Delete \"{track.Title}\"?\n\nThis will remove the audio file and database entry.")
            .ShowDialog<bool>(owner);
        if (!confirmed) return;

        if (_playingTrackId == track.Id)
        {
            StopPlayback();
            NowPlayingText.Text = "";
        }

        Db.DeleteTrack(track.Id);

        try
        {
            var filePath = Path.Combine(MusicDir, track.FileName);
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"File could not be deleted: {ex.Message}";
        }

        RefreshTrackList();
    }

    private void OnPlayClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;

        //StopPlayback();

        var track = _filteredItems[idx].Track;
        try
        {
            _audioStream = new MediaFoundationReader(Path.Combine(MusicDir, track.FileName));
            _player = new WaveOutEvent();
            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(_audioStream);
            _player.Play();
            _playingTrackId = track.Id;
            NowPlayingText.Text = track.Title;
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
        Dispatcher.UIThread.Post(() =>
        {
            NowPlayingText.Text = "";
            _playingTrackId = -1;
        });
    }

    public void StopPlayback()
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
        _playingTrackId = -1;
    }

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m}:{s:D2}";
    }
}
