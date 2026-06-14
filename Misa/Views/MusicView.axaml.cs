using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Misa.Models;
using Misa.Music.Models;
using Misa.Music.Services;
using NAudio.Wave;

namespace Misa.Views;

public partial class MusicView : UserControl
{
    private const string MusicDir = @"D:\media\music";

    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

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
        _progressTimer.Tick += OnProgressTick;
        GenreFilter.SelectionChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        StyleFilter.SelectionChanged += (_, _) => ApplyFilter();
        try
        {
            MusicLibraryService.Current.Initialize();
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
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        GenreFilter.Placeholder = "Genres";
        GenreFilter.SetItems(_genres.Select(g => g.Name));
        RatingFilter.Placeholder = "Ratings";
        RatingFilter.SetItems(_ratings.Select(r => r.Name));
        StyleFilter.Placeholder = "Styles";
        StyleFilter.SetItems(_styles.Select(s => s.Name));
    }

    private void RefreshTrackList()
    {
        var tracks = MusicLibraryService.Current.GetTracks();
        var allStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
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
        var selGenreIds = SelectedIds(GenreFilter.SelectedItems, _genres, g => g.Name, g => g.Id);
        var selRatingIds = SelectedIds(RatingFilter.SelectedItems, _ratings, r => r.Name, r => r.Id);
        var selStyleIds = SelectedIds(StyleFilter.SelectedItems, _styles, s => s.Name, s => s.Id);

        _filteredItems = _allItems.Where(item =>
            (selGenreIds.Count == 0 || selGenreIds.Contains(item.GenreId)) &&
            (selRatingIds.Count == 0 || selRatingIds.Contains(item.RatingId)) &&
            (selStyleIds.Count == 0 || item.StyleIds.Any(id => selStyleIds.Contains(id)))
        ).ToList();

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _playingTrackId;

        FileList.ItemsSource = _filteredItems;
    }

    private static HashSet<int> SelectedIds<T>(IReadOnlySet<string> selected, List<T> source,
        Func<T, string> nameOf, Func<T, int> idOf)
    {
        if (selected.Count == 0) return [];
        return source.Where(item => selected.Contains(nameOf(item))).Select(idOf).ToHashSet();
    }

    public void Refresh()
    {
        NowPlayingText.Text = "";
        LoadLookups();
        RefreshTrackList();
    }

    public void RefreshFilters()
    {
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
            StopPlayback();

        var result = MusicLibraryService.Current.DeleteTrack(track.Id, track.FileName);
        if (result.FileError != null)
            StatusText.Text = $"File could not be deleted: {result.FileError}";

        RefreshTrackList();
    }

    private void OnPlayClicked(object? sender, RoutedEventArgs e) => StartPlayback();

    private void OnListDoubleTapped(object? sender, TappedEventArgs e) => StartPlayback();

    private void StartPlayback()
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;

        if (_player != null)
        {
            _player.PlaybackStopped -= OnPlaybackStopped;
            _player.Stop();
            _player.Dispose();
            _audioStream?.Dispose();
            _player = null;
            _audioStream = null;
        }
        _progressTimer.Stop();

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
            PlaybackInfoPanel.IsVisible = true;
            _progressTimer.Start();
            RefreshPlayingMarkers();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
            _playingTrackId = -1;
        }
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e) => StopPlayback();

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        var stoppedPlayer = sender as IWavePlayer;
        Dispatcher.UIThread.Post(() =>
        {
            if (_player == null || _player != stoppedPlayer) return;
            _player.PlaybackStopped -= OnPlaybackStopped;
            _player.Dispose();
            _audioStream?.Dispose();
            _player = null;
            _audioStream = null;
            _playingTrackId = -1;
            _progressTimer.Stop();
            ResetPlaybackUI();
            RefreshPlayingMarkers();
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
        _progressTimer.Stop();
        ResetPlaybackUI();
        RefreshPlayingMarkers();
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (_audioStream == null) return;
        var current = _audioStream.CurrentTime;
        var total = _audioStream.TotalTime;
        PlaybackTimeText.Text = $"{FormatDuration((int)current.TotalSeconds)} / {FormatDuration((int)total.TotalSeconds)}";
        PlaybackProgress.Value = total.TotalSeconds > 0
            ? current.TotalSeconds / total.TotalSeconds * 100
            : 0;
    }

    private void ResetPlaybackUI()
    {
        NowPlayingText.Text = "";
        PlaybackProgress.Value = 0;
        PlaybackTimeText.Text = "";
        PlaybackInfoPanel.IsVisible = false;
    }

    private void RefreshPlayingMarkers()
    {
        if (_filteredItems.Count == 0) return;

        var selectedId = FileList.SelectedIndex >= 0 && FileList.SelectedIndex < _filteredItems.Count
            ? _filteredItems[FileList.SelectedIndex].Track.Id : -1;

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _playingTrackId;

        FileList.ItemsSource = _filteredItems.ToList();

        if (selectedId >= 0)
        {
            var idx = _filteredItems.FindIndex(i => i.Track.Id == selectedId);
            if (idx >= 0) FileList.SelectedIndex = idx;
        }
    }

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m}:{s:D2}";
    }
}
