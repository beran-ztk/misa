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
    private enum PlaybackState { Stopped, Playing, Paused }
    private enum RepeatMode { None, RepeatOne, RepeatAll }

    private readonly DispatcherTimer _progressTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };

    private IWavePlayer? _player;
    private WaveStream? _audioStream;
    private int _playingTrackId = -1;
    private PlaybackState _state = PlaybackState.Stopped;
    private bool _isSeeking;
    private bool _autoplay;
    private RepeatMode _repeatMode = RepeatMode.None;

    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    // _filteredItems is the play context: tracks visible after all filters, search, and sort.
    private List<TrackDisplayItem> _filteredItems = [];

    public MusicView()
    {
        InitializeComponent();
        _progressTimer.Tick += OnProgressTick;
        PlaybackSlider.AddHandler(InputElement.PointerPressedEvent, OnSliderPointerPressed, RoutingStrategies.Tunnel);
        PlaybackSlider.AddHandler(InputElement.PointerReleasedEvent, OnSliderPointerReleased, RoutingStrategies.Tunnel);

        SortFieldCombo.ItemsSource = new[] { "Title", "Rating", "Downloaded", "Duration" };
        SortFieldCombo.SelectedIndex = 2;
        SortDirectionCombo.ItemsSource = new[] { "Ascending", "Descending" };
        SortDirectionCombo.SelectedIndex = 1;

        RepeatModeCombo.ItemsSource = new[] { "No repeat", "Repeat one", "Repeat all" };
        RepeatModeCombo.SelectedIndex = 0;

        SearchBox.TextChanged += (_, _) => ApplyFilter();
        SortFieldCombo.SelectionChanged += (_, _) => ApplyFilter();
        SortDirectionCombo.SelectionChanged += (_, _) => ApplyFilter();
        GenreFilter.SelectionChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        StyleFilter.SelectionChanged += (_, _) => ApplyFilter();
        AutoplayCheckBox.IsCheckedChanged += (_, _) => _autoplay = AutoplayCheckBox.IsChecked == true;
        RepeatModeCombo.SelectionChanged += (_, _) =>
            _repeatMode = RepeatModeCombo.SelectedIndex switch
            {
                1 => RepeatMode.RepeatOne,
                2 => RepeatMode.RepeatAll,
                _ => RepeatMode.None,
            };

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

    // --- Track list ---

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
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        var genreMap = _genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = _ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = _styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);
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

        var ratingSortOrders = _ratings.ToDictionary(r => r.Id, r => r.SortOrder);
        var itemById = _allItems.ToDictionary(i => i.Track.Id);

        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackStyleIds,
            ratingSortOrders,
            selGenreIds, selRatingIds, selStyleIds,
            SearchBox.Text,
            GetSortField(), GetSortDirection());

        _filteredItems = filtered
            .Where(t => itemById.ContainsKey(t.Id))
            .Select(t => itemById[t.Id])
            .ToList();

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

    private TrackSortField GetSortField() => SortFieldCombo.SelectedIndex switch
    {
        0 => TrackSortField.Title,
        1 => TrackSortField.Rating,
        2 => TrackSortField.DownloadedAt,
        3 => TrackSortField.Duration,
        _ => TrackSortField.DownloadedAt,
    };

    private TrackSortDirection GetSortDirection() =>
        SortDirectionCombo.SelectedIndex == 1 ? TrackSortDirection.Descending : TrackSortDirection.Ascending;

    public IReadOnlyList<TrackDisplayItem> GetPlayContext() => _filteredItems;

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

    // --- Dialogs ---

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

    // --- Playback control ---

    private void OnPlayClicked(object? sender, RoutedEventArgs e) => StartPlayback();
    private void OnListDoubleTapped(object? sender, TappedEventArgs e) => StartPlayback();
    private void OnPreviousClicked(object? sender, RoutedEventArgs e) => NavigatePrevious();
    private void OnNextClicked(object? sender, RoutedEventArgs e) => NavigateNext(isManual: true);

    private void OnPauseResumeClicked(object? sender, RoutedEventArgs e)
    {
        if (_state == PlaybackState.Playing)
            PausePlayback();
        else if (_state == PlaybackState.Paused)
            ResumePlayback();
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e) => StopPlayback();

    private void StartPlayback()
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;
        PlayTrackAt(idx);
    }

    private void PlayTrackAt(int filteredIndex)
    {
        if (filteredIndex < 0 || filteredIndex >= _filteredItems.Count) return;

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
        _isSeeking = false;

        var track = _filteredItems[filteredIndex].Track;
        FileList.SelectedIndex = filteredIndex;

        try
        {
            _audioStream = new MediaFoundationReader(
                Path.Combine(MusicLibraryService.Current.MusicDirectory, track.FileName));
            _player = new WaveOutEvent();
            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(_audioStream);
            _player.Play();
            _playingTrackId = track.Id;
            _state = PlaybackState.Playing;
            NowPlayingText.Text = track.Title;
            PlaybackInfoPanel.IsVisible = true;
            UpdateProgressDisplay();
            _progressTimer.Start();
            UpdateButtonStates();
            RefreshPlayingMarkers();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
            FullStop();
        }
    }

    private void PausePlayback()
    {
        _player?.Pause();
        _progressTimer.Stop();
        _state = PlaybackState.Paused;
        UpdateButtonStates();
    }

    private void ResumePlayback()
    {
        _player?.Play();
        _progressTimer.Start();
        _state = PlaybackState.Playing;
        UpdateButtonStates();
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
        _state = PlaybackState.Stopped;
        _isSeeking = false;
        _progressTimer.Stop();
        FullStop();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        var stoppedPlayer = sender as IWavePlayer;
        Dispatcher.UIThread.Post(() =>
        {
            if (_player == null || _player != stoppedPlayer) return;

            // Tear down player machinery; keep _playingTrackId so NavigateNext can use it.
            _player.PlaybackStopped -= OnPlaybackStopped;
            _player.Dispose();
            _audioStream?.Dispose();
            _player = null;
            _audioStream = null;
            _state = PlaybackState.Stopped;
            _isSeeking = false;
            _progressTimer.Stop();

            if (_autoplay)
                NavigateNext(isManual: false);
            else
                FullStop();
        });
    }

    // --- Navigation ---

    private int GetCurrentPlayIndex() =>
        _playingTrackId < 0 ? -1 : _filteredItems.FindIndex(i => i.Track.Id == _playingTrackId);

    private void NavigateNext(bool isManual)
    {
        if (_filteredItems.Count == 0)
        {
            StatusText.Text = "No tracks in the current view.";
            FullStop();
            return;
        }

        var currentIdx = GetCurrentPlayIndex();

        // RepeatOne only takes effect on autoplay, not manual Next.
        if (_repeatMode == RepeatMode.RepeatOne && !isManual)
        {
            PlayTrackAt(currentIdx >= 0 ? currentIdx : 0);
            return;
        }

        int nextIdx;
        if (_repeatMode == RepeatMode.RepeatAll)
        {
            nextIdx = currentIdx < 0 ? 0 : (currentIdx + 1) % _filteredItems.Count;
        }
        else
        {
            // None, or RepeatOne with manual press (behaves like None for navigation).
            if (currentIdx >= 0)
            {
                nextIdx = currentIdx + 1;
                if (nextIdx >= _filteredItems.Count)
                {
                    FullStop();
                    return;
                }
            }
            else if (_playingTrackId < 0)
            {
                // Nothing playing: use selected track as the starting point.
                var selIdx = FileList.SelectedIndex;
                nextIdx = selIdx >= 0 && selIdx < _filteredItems.Count ? selIdx : 0;
            }
            else
            {
                // Playing track is no longer visible in the filtered list.
                nextIdx = 0;
            }
        }

        PlayTrackAt(nextIdx);
    }

    private void NavigatePrevious()
    {
        if (_filteredItems.Count == 0) return;

        var currentIdx = GetCurrentPlayIndex();

        int prevIdx;
        if (_repeatMode == RepeatMode.RepeatAll)
        {
            prevIdx = currentIdx <= 0 ? _filteredItems.Count - 1 : currentIdx - 1;
        }
        else
        {
            if (currentIdx < 0)
            {
                // Nothing playing or not visible: use selected as reference.
                var selIdx = FileList.SelectedIndex;
                if (selIdx <= 0) return;
                prevIdx = selIdx - 1;
            }
            else if (currentIdx == 0)
            {
                return; // Already at the first track.
            }
            else
            {
                prevIdx = currentIdx - 1;
            }
        }

        PlayTrackAt(prevIdx);
    }

    // Clears all playing state and resets the UI. Does NOT touch _player/_audioStream.
    private void FullStop()
    {
        _playingTrackId = -1;
        _state = PlaybackState.Stopped;
        ResetPlaybackUI();
        UpdateButtonStates();
        RefreshPlayingMarkers();
    }

    // --- Progress ---

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (_isSeeking) return;
        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay()
    {
        if (_audioStream == null) return;
        var current = _audioStream.CurrentTime;
        var total = _audioStream.TotalTime;
        PlaybackTimeText.Text = $"{FormatDuration(current)} / {FormatDuration(total)}";
        if (!_isSeeking)
            PlaybackSlider.Value = total.TotalSeconds > 0
                ? current.TotalSeconds / total.TotalSeconds * 100
                : 0;
    }

    private void ResetPlaybackUI()
    {
        NowPlayingText.Text = "";
        PlaybackSlider.Value = 0;
        PlaybackTimeText.Text = "";
        PlaybackInfoPanel.IsVisible = false;
    }

    // --- Seeking ---

    private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_audioStream == null) return;
        _isSeeking = true;
        _progressTimer.Stop();
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_audioStream == null)
        {
            _isSeeking = false;
            return;
        }

        var totalSeconds = _audioStream.TotalTime.TotalSeconds;
        var targetSeconds = Math.Clamp(PlaybackSlider.Value / 100.0 * totalSeconds, 0, totalSeconds);

        if (_state == PlaybackState.Playing)
        {
            _player?.Pause();
            _audioStream.CurrentTime = TimeSpan.FromSeconds(targetSeconds);
            _player?.Play();
        }
        else
        {
            _audioStream.CurrentTime = TimeSpan.FromSeconds(targetSeconds);
        }

        PlaybackTimeText.Text = $"{FormatDuration(_audioStream.CurrentTime)} / {FormatDuration(_audioStream.TotalTime)}";
        _isSeeking = false;

        if (_state == PlaybackState.Playing)
            _progressTimer.Start();
    }

    // --- UI helpers ---

    private void UpdateButtonStates()
    {
        PauseResumeBtn.IsVisible = _state != PlaybackState.Stopped;
        PauseResumeBtn.Content = _state == PlaybackState.Paused ? "Resume" : "Pause";
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

    // --- Formatting ---

    private static string FormatDuration(TimeSpan t) => FormatDuration((int)t.TotalSeconds);

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m:D2}:{s:D2}";
    }
}
