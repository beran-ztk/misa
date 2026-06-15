using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
    private readonly Random _rng = new();
    // Track IDs played during shuffle, for Previous to walk back through (capped at 50).
    private readonly List<int> _shuffleHistory = [];

    private IWavePlayer? _player;
    private WaveStream? _audioStream;
    private int _playingTrackId = -1;
    private PlaybackState _state = PlaybackState.Stopped;
    private bool _isSeeking;
    private bool _autoplay;
    private RepeatMode _repeatMode = RepeatMode.None;
    private float _volume = 1.0f;
    private bool _muted;
    private bool _shuffle;
    private bool _loadingSettings;

    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    // _filteredItems is the play context: tracks visible after all filters, search, and sort.
    private List<TrackDisplayItem> _filteredItems = [];

    private record FilterGroupControls(MultiSelectFilterControl GenreCtrl, MultiSelectFilterControl StyleCtrl);
    private readonly List<FilterGroupControls> _filterGroups = [];

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
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        ReEvalFilterCheckBox.IsCheckedChanged += (_, _) => ApplyFilter();

        AutoplayCheckBox.IsCheckedChanged += (_, _) =>
        {
            _autoplay = AutoplayCheckBox.IsChecked == true;
            SavePlayerSettings();
        };
        RepeatModeCombo.SelectionChanged += (_, _) =>
        {
            _repeatMode = RepeatModeCombo.SelectedIndex switch
            {
                1 => RepeatMode.RepeatOne,
                2 => RepeatMode.RepeatAll,
                _ => RepeatMode.None,
            };
            SavePlayerSettings();
        };

        VolumeSlider.ValueChanged += (_, _) =>
        {
            _volume = (float)(VolumeSlider.Value / 100.0);
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            if (!_muted) ApplyVolume();
            SavePlayerSettings();
        };
        MuteCheckBox.IsCheckedChanged += (_, _) =>
        {
            _muted = MuteCheckBox.IsChecked == true;
            ApplyVolume();
            SavePlayerSettings();
        };
        ShuffleCheckBox.IsCheckedChanged += (_, _) =>
        {
            _shuffle = ShuffleCheckBox.IsChecked == true;
            if (!_shuffle) _shuffleHistory.Clear();
            SavePlayerSettings();
        };

        LoadPlayerSettings();

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
        AddFilterGroup();
        RefreshTrackList();
    }

    private void LoadPlayerSettings()
    {
        _loadingSettings = true;
        try
        {
            var s = MusicLibraryService.Current.GetSettings();
            VolumeSlider.Value = s.Volume;
            VolumeText.Text = $"{s.Volume}%"; // explicit — event may not fire if value unchanged
            MuteCheckBox.IsChecked = s.IsMuted;
            ShuffleCheckBox.IsChecked = s.ShuffleEnabled;
            AutoplayCheckBox.IsChecked = s.AutoplayEnabled;
            RepeatModeCombo.SelectedIndex = s.RepeatMode switch
            {
                "RepeatOne" => 1,
                "RepeatAll" => 2,
                _ => 0,
            };
        }
        finally
        {
            _loadingSettings = false;
        }
    }

    private void SavePlayerSettings()
    {
        if (_loadingSettings) return;
        MusicLibraryService.Current.SavePlayerSettings(
            volume: (int)VolumeSlider.Value,
            isMuted: _muted,
            shuffleEnabled: _shuffle,
            autoplayEnabled: _autoplay,
            repeatMode: _repeatMode switch
            {
                RepeatMode.RepeatOne => "RepeatOne",
                RepeatMode.RepeatAll => "RepeatAll",
                _ => "None",
            });
    }

    // --- Track list ---

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();

        RatingFilter.Placeholder = "Ratings";
        RatingFilter.SetItems(_ratings.Select(r => r.Name));

        // Keep genre/style items in existing filter groups up to date
        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.SetItems(_genres.Select(g => g.Name));
            fg.StyleCtrl.SetItems(_styles.Select(s => s.Name));
        }
    }

    private void RefreshTrackList()
    {
        var tracks = MusicLibraryService.Current.GetTracks();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        var genreMap = _genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = _ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = _styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var genreIds = _allTrackGenreIds.GetValueOrDefault(t.Id, []);
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);

            var genreNames = genreIds
                .Select(id => genreMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0)
                .Order();
            var styleNames = styleIds
                .Select(id => styleMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0)
                .Order();

            var parts = new List<string>();
            var genreStr = string.Join(", ", genreNames);
            if (genreStr.Length > 0) parts.Add(genreStr);
            parts.Add(ratingMap.GetValueOrDefault(t.RatingId, "?"));
            if (t.DurationSeconds.HasValue)
                parts.Add(FormatDuration(t.DurationSeconds.Value));
            var styleStr = string.Join(", ", styleNames);
            if (styleStr.Length > 0) parts.Add(styleStr);
            if (t.ReEvaluationNeeded) parts.Add("[re-eval]");

            return new TrackDisplayItem(t, string.Join(" · ", parts), genreIds, styleIds);
        }).ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var selRatingIds = SelectedIds(RatingFilter.SelectedItems, _ratings, r => r.Name, r => r.Id);
        var ratingSortOrders = _ratings.ToDictionary(r => r.Id, r => r.SortOrder);
        var itemById = _allItems.ToDictionary(i => i.Track.Id);

        var groups = _filterGroups
            .Select(fg => new FilterGroup(
                SelectedIds(fg.GenreCtrl.SelectedItems, _genres, g => g.Name, g => g.Id),
                SelectedIds(fg.StyleCtrl.SelectedItems, _styles, s => s.Name, s => s.Id)))
            .ToList();

        bool? reEvalFilter = ReEvalFilterCheckBox.IsChecked == true ? true : null;

        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackGenreIds,
            _allTrackStyleIds,
            ratingSortOrders,
            selRatingIds,
            groups,
            reEvalFilter,
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

    // --- Filter groups ---

    private void OnAddFilterGroupClicked(object? sender, RoutedEventArgs e)
    {
        AddFilterGroup();
        ApplyFilter();
    }

    private void AddFilterGroup()
    {
        var genreCtrl = new MultiSelectFilterControl { Placeholder = "Genres" };
        genreCtrl.SetItems(_genres.Select(g => g.Name));
        genreCtrl.SelectionChanged += (_, _) => ApplyFilter();

        var styleCtrl = new MultiSelectFilterControl { Placeholder = "Styles" };
        styleCtrl.SetItems(_styles.Select(s => s.Name));
        styleCtrl.SelectionChanged += (_, _) => ApplyFilter();

        var fg = new FilterGroupControls(genreCtrl, styleCtrl);
        _filterGroups.Add(fg);

        var removeBtn = new Button { Content = "×", Padding = new Thickness(6, 0) };
        removeBtn.Click += (_, _) => RemoveFilterGroup(fg);

        var genreWrapper = new StackPanel { Spacing = 2, MinWidth = 150 };
        genreWrapper.Children.Add(new TextBlock { Text = "Genre", FontSize = 11, Opacity = 0.55 });
        genreWrapper.Children.Add(genreCtrl);

        var styleWrapper = new StackPanel { Spacing = 2, MinWidth = 150 };
        styleWrapper.Children.Add(new TextBlock { Text = "Style", FontSize = 11, Opacity = 0.55 });
        styleWrapper.Children.Add(styleCtrl);

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(genreWrapper);
        row.Children.Add(styleWrapper);
        row.Children.Add(removeBtn);

        FilterGroupsPanel.Children.Add(row);
    }

    private void RemoveFilterGroup(FilterGroupControls fg)
    {
        var idx = _filterGroups.IndexOf(fg);
        if (idx < 0) return;
        _filterGroups.RemoveAt(idx);
        FilterGroupsPanel.Children.RemoveAt(idx);
        if (_filterGroups.Count == 0)
            AddFilterGroup();
        ApplyFilter();
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
            ApplyVolume();
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

    // --- Volume ---

    private void ApplyVolume()
    {
        if (_player is WaveOutEvent waveOut)
            waveOut.Volume = _muted ? 0f : _volume;
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

        // RepeatOne on autoplay always replays the same track — shuffle doesn't apply.
        if (_repeatMode == RepeatMode.RepeatOne && !isManual)
        {
            PlayTrackAt(currentIdx >= 0 ? currentIdx : 0);
            return;
        }

        // Shuffle: pick a random track, avoiding an immediate repeat when count > 1.
        if (_shuffle)
        {
            // Push current track to history before leaving it.
            if (_playingTrackId >= 0)
            {
                _shuffleHistory.Add(_playingTrackId);
                if (_shuffleHistory.Count > 50) _shuffleHistory.RemoveAt(0);
            }

            int nextIdx;
            if (_filteredItems.Count == 1)
            {
                nextIdx = 0;
            }
            else
            {
                do { nextIdx = _rng.Next(_filteredItems.Count); }
                while (nextIdx == currentIdx);
            }
            PlayTrackAt(nextIdx);
            return;
        }

        // Linear navigation.
        int nextLinearIdx;
        if (_repeatMode == RepeatMode.RepeatAll)
        {
            nextLinearIdx = currentIdx < 0 ? 0 : (currentIdx + 1) % _filteredItems.Count;
        }
        else
        {
            if (currentIdx >= 0)
            {
                nextLinearIdx = currentIdx + 1;
                if (nextLinearIdx >= _filteredItems.Count)
                {
                    FullStop();
                    return;
                }
            }
            else if (_playingTrackId < 0)
            {
                // Nothing playing: use selected track as the starting point.
                var selIdx = FileList.SelectedIndex;
                nextLinearIdx = selIdx >= 0 && selIdx < _filteredItems.Count ? selIdx : 0;
            }
            else
            {
                // Playing track is no longer visible in the filtered list.
                nextLinearIdx = 0;
            }
        }

        PlayTrackAt(nextLinearIdx);
    }

    private void NavigatePrevious()
    {
        if (_filteredItems.Count == 0) return;

        // Shuffle: walk back through history.
        if (_shuffle && _shuffleHistory.Count > 0)
        {
            while (_shuffleHistory.Count > 0)
            {
                var histId = _shuffleHistory[^1];
                _shuffleHistory.RemoveAt(_shuffleHistory.Count - 1);
                var histIdx = _filteredItems.FindIndex(i => i.Track.Id == histId);
                if (histIdx >= 0)
                {
                    PlayTrackAt(histIdx);
                    return;
                }
                // Track no longer visible — keep popping.
            }
            // History exhausted; fall through to linear.
        }

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
