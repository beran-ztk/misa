using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Misa.Models;
using Misa.Music.Models;
using Misa.Music.Services;
using Misa.Services;

namespace Misa.Views;

public partial class MusicView : UserControl
{
    private enum RepeatMode { None, RepeatOne, RepeatAll }

    // Engine
    private readonly PlaybackEngine _engine = new();
    private bool _isSeeking;

    // Playback settings (mirrored from MusicSettings for quick access)
    private bool _autoplay;
    private bool _crossfadeEnabled;
    private int _crossfadeDurationSeconds = 10;
    private int _manualFadeDurationSeconds = 2;
    private bool _showUpcomingTrackBar = true;
    private RepeatMode _repeatMode = RepeatMode.None;
    private float _volume = 1.0f;
    private bool _muted;
    private bool _shuffle;
    private bool _loadingSettings;

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private int _nextTrackIndex = -1;

    // Shuffle history (for Previous to walk back)
    private readonly List<int> _shuffleHistory = [];
    private readonly Random _rng = new();

    // Track list data
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];
    private List<Language> _languages = [];
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private Dictionary<int, List<int>> _allTrackLanguageIds = [];
    private List<TrackDisplayItem> _filteredItems = [];

    private record FilterGroupControls(MultiSelectFilterControl GenreCtrl, MultiSelectFilterControl StyleCtrl, MultiSelectFilterControl LanguageCtrl);
    private readonly List<FilterGroupControls> _filterGroups = [];

    public MusicView()
    {
        InitializeComponent();

        // Engine events
        _engine.StateChanged += OnEngineStateChanged;
        _engine.TrackNaturallyEnded += OnTrackNaturallyEnded;
        _engine.ProgressUpdated += OnProgressUpdated;

        // Seeking
        PlaybackSlider.AddHandler(InputElement.PointerPressedEvent, OnSliderPointerPressed, RoutingStrategies.Tunnel);
        PlaybackSlider.AddHandler(InputElement.PointerReleasedEvent, OnSliderPointerReleased, RoutingStrategies.Tunnel);

        // Sort/search
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

        // Player controls
        AutoplayCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _autoplay = AutoplayCheckBox.IsChecked == true;
            SavePlayerSettings();
        };
        CrossfadeCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _crossfadeEnabled = CrossfadeCheckBox.IsChecked == true;
            SavePlayerSettings();
        };
        RepeatModeCombo.SelectionChanged += (_, _) =>
        {
            if (_loadingSettings) return;
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
            if (_loadingSettings) return;
            _volume = (float)(VolumeSlider.Value / 100.0);
            VolumeText.Text = $"{(int)VolumeSlider.Value}%";
            _engine.MasterVolume = _volume;
            if (!_muted) _engine.ApplyVolume();
            SavePlayerSettings();
        };
        MuteCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _muted = MuteCheckBox.IsChecked == true;
            _engine.Muted = _muted;
            _engine.ApplyVolume();
            SavePlayerSettings();
        };
        ShuffleCheckBox.IsCheckedChanged += (_, _) =>
        {
            if (_loadingSettings) return;
            _shuffle = ShuffleCheckBox.IsChecked == true;
            if (!_shuffle) _shuffleHistory.Clear();
            SavePlayerSettings();
        };

        LoadPlayerSettings();

        try { MusicLibraryService.Current.Initialize(); }
        catch (Exception ex) { StatusText.Text = $"Database error: {ex.Message}"; return; }

        LoadLookups();
        AddFilterGroup();
        RefreshTrackList();
    }

    // --- Settings ---

    private void LoadPlayerSettings()
    {
        _loadingSettings = true;
        try
        {
            var s = MusicLibraryService.Current.GetSettings();
            VolumeSlider.Value = s.Volume;
            VolumeText.Text = $"{s.Volume}%";
            MuteCheckBox.IsChecked = s.IsMuted;
            ShuffleCheckBox.IsChecked = s.ShuffleEnabled;
            AutoplayCheckBox.IsChecked = s.AutoplayEnabled;
            CrossfadeCheckBox.IsChecked = s.CrossfadeEnabled;
            RepeatModeCombo.SelectedIndex = s.RepeatMode switch
            {
                "RepeatOne" => 1,
                "RepeatAll" => 2,
                _ => 0,
            };
            _volume = s.Volume / 100f;
            _muted = s.IsMuted;
            _shuffle = s.ShuffleEnabled;
            _autoplay = s.AutoplayEnabled;
            _crossfadeEnabled = s.CrossfadeEnabled;
            _crossfadeDurationSeconds = Math.Clamp(s.CrossfadeDurationSeconds, 0, 30);
            _manualFadeDurationSeconds = Math.Clamp(s.ManualSwitchFadeDurationSeconds, 0, 10);
            _showUpcomingTrackBar = s.ShowUpcomingTrackBar;
            _repeatMode = s.RepeatMode switch
            {
                "RepeatOne" => RepeatMode.RepeatOne,
                "RepeatAll" => RepeatMode.RepeatAll,
                _ => RepeatMode.None,
            };
            _engine.MasterVolume = _volume;
            _engine.Muted = _muted;
        }
        finally { _loadingSettings = false; }
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
            },
            crossfadeEnabled: _crossfadeEnabled,
            showUpcomingTrackBar: _showUpcomingTrackBar);
    }

    // --- Track list ---

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _languages = MusicLibraryService.Current.GetLanguages();

        RatingFilter.Placeholder = "Ratings";
        RatingFilter.SetItems(_ratings.Select(r => r.Name));

        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.SetItems(_genres.Select(g => g.Name));
            fg.StyleCtrl.SetItems(_styles.Select(s => s.Name));
            fg.LanguageCtrl.SetItems(_languages.Select(l => l.Name));
        }
    }

    private void RefreshTrackList()
    {
        var tracks = MusicLibraryService.Current.GetTracks();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackLanguageIds = MusicLibraryService.Current.GetAllTrackLanguageIds();
        var genreMap = _genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = _ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = _styles.ToDictionary(s => s.Id, s => s.Name);
        var languageMap = _languages.ToDictionary(l => l.Id, l => l.Name);

        _allItems = tracks.Select(t =>
        {
            var genreIds = _allTrackGenreIds.GetValueOrDefault(t.Id, []);
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);
            var languageIds = _allTrackLanguageIds.GetValueOrDefault(t.Id, []);

            var parts = new List<string>();
            var genreStr = string.Join(", ", genreIds
                .Select(id => genreMap.GetValueOrDefault(id, "")).Where(n => n.Length > 0).Order());
            if (genreStr.Length > 0) parts.Add(genreStr);
            parts.Add(ratingMap.GetValueOrDefault(t.RatingId, "?"));
            if (t.DurationSeconds.HasValue) parts.Add(FormatDuration(t.DurationSeconds.Value));
            var styleStr = string.Join(", ", styleIds
                .Select(id => styleMap.GetValueOrDefault(id, "")).Where(n => n.Length > 0).Order());
            if (styleStr.Length > 0) parts.Add(styleStr);
            var langStr = string.Join(", ", languageIds
                .Select(id => languageMap.GetValueOrDefault(id, "")).Where(n => n.Length > 0).Order());
            if (langStr.Length > 0) parts.Add(langStr);
            if (t.ReEvaluationNeeded) parts.Add("[re-eval]");

            return new TrackDisplayItem(t, string.Join(" · ", parts), genreIds, styleIds, languageIds);
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
                SelectedIds(fg.StyleCtrl.SelectedItems, _styles, s => s.Name, s => s.Id),
                SelectedIds(fg.LanguageCtrl.SelectedItems, _languages, l => l.Name, l => l.Id)))
            .ToList();

        bool? reEvalFilter = ReEvalFilterCheckBox.IsChecked == true ? true : null;

        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackGenreIds,
            _allTrackStyleIds,
            _allTrackLanguageIds,
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
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        FileList.ItemsSource = _filteredItems;
        RefreshNextTrackPreview();
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
        LoadPlayerSettings();
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

        var languageCtrl = new MultiSelectFilterControl { Placeholder = "Languages" };
        languageCtrl.SetItems(_languages.Select(l => l.Name));
        languageCtrl.SelectionChanged += (_, _) => ApplyFilter();

        var fg = new FilterGroupControls(genreCtrl, styleCtrl, languageCtrl);
        _filterGroups.Add(fg);

        var removeBtn = new Button { Content = "×", Padding = new Thickness(6, 0) };
        removeBtn.Click += (_, _) => RemoveFilterGroup(fg);

        var genreWrapper = new StackPanel { Spacing = 2, MinWidth = 150 };
        genreWrapper.Children.Add(new TextBlock { Text = "Genre", FontSize = 11, Opacity = 0.55 });
        genreWrapper.Children.Add(genreCtrl);

        var styleWrapper = new StackPanel { Spacing = 2, MinWidth = 150 };
        styleWrapper.Children.Add(new TextBlock { Text = "Style", FontSize = 11, Opacity = 0.55 });
        styleWrapper.Children.Add(styleCtrl);

        var languageWrapper = new StackPanel { Spacing = 2, MinWidth = 150 };
        languageWrapper.Children.Add(new TextBlock { Text = "Language", FontSize = 11, Opacity = 0.55 });
        languageWrapper.Children.Add(languageCtrl);

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(genreWrapper);
        row.Children.Add(styleWrapper);
        row.Children.Add(languageWrapper);
        row.Children.Add(removeBtn);

        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(14, 128, 128, 128)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(24, 128, 128, 128)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(10, 7),
            Child = row,
        };
        FilterGroupsPanel.Children.Add(card);
    }

    private void RemoveFilterGroup(FilterGroupControls fg)
    {
        var idx = _filterGroups.IndexOf(fg);
        if (idx < 0) return;
        _filterGroups.RemoveAt(idx);
        FilterGroupsPanel.Children.RemoveAt(idx);
        if (_filterGroups.Count == 0) AddFilterGroup();
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

        if (_engine.ActiveTrackId == track.Id) _engine.Stop();

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
        if (_engine.State == EngineState.Playing) _engine.Pause();
        else if (_engine.State == EngineState.Paused) _engine.Resume();
        UpdateButtonStates();
    }

    private void OnStopClicked(object? sender, RoutedEventArgs e)
    {
        _engine.Stop();
        // StateChanged fires FullStop via OnEngineStateChanged.
    }

    private void StartPlayback()
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;
        PlayTrackAt(idx, isCrossfade: false);
    }

    private void PlayTrackAt(int filteredIndex, bool isCrossfade)
    {
        if (filteredIndex < 0 || filteredIndex >= _filteredItems.Count) return;

        var track = _filteredItems[filteredIndex].Track;
        var filePath = Path.Combine(MusicLibraryService.Current.MusicDirectory, track.FileName);

        bool wasPlaying = _engine.State != EngineState.Stopped;
        float fadeOut = isCrossfade ? _crossfadeDurationSeconds
                      : wasPlaying ? _manualFadeDurationSeconds : 0f;
        // Crossfade: new track fades in over the crossfade window.
        // Manual switch: new track also fades in (same duration as fade-out) so it
        // never starts at full volume and does not inherit any outgoing track's volume.
        // Cold start (nothing was playing): immediate full volume, no fade needed.
        float fadeIn = isCrossfade ? _crossfadeDurationSeconds
                     : wasPlaying ? _manualFadeDurationSeconds : 0f;

        try
        {
            _isSeeking = false;
            _engine.Play(filePath, track.Id, fadeOut, fadeIn);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
            return;
        }

        FileList.SelectedIndex = filteredIndex;

        // If this is a shuffle play, push to history.
        if (_shuffle && wasPlaying)
        {
            _shuffleHistory.Add(_engine.ActiveTrackId);
            if (_shuffleHistory.Count > 50) _shuffleHistory.RemoveAt(0);
        }

        NowPlayingText.Text = track.Title;
        PlaybackInfoPanel.IsVisible = true;
        _nextTrackIndex = PeekNextTrackIndex(filteredIndex);
        UpdateUpcomingBar();
        UpdateButtonStates();
        RefreshPlayingMarkers();
    }

    // --- Engine events ---

    private void OnEngineStateChanged()
    {
        UpdateButtonStates();
        if (_engine.State == EngineState.Stopped)
        {
            _nextTrackIndex = -1;
            _crossfadeTriggered = false;
            _lastKnownActiveId = -1;
            ResetPlaybackUI();
            RefreshPlayingMarkers();
            UpdateUpcomingBar();
        }
    }

    private void OnTrackNaturallyEnded()
    {
        if (_autoplay)
            NavigateNext(isManual: false);
        else
            FullStop();
    }

    private void OnProgressUpdated()
    {
        // Detect track change (e.g. crossfade started a new primary).
        if (_engine.ActiveTrackId != _lastKnownActiveId)
        {
            _lastKnownActiveId = _engine.ActiveTrackId;
            _crossfadeTriggered = false;
        }

        // Update progress UI.
        if (!_isSeeking && _engine.TotalTime.TotalSeconds > 0)
            PlaybackSlider.Value = _engine.CurrentTime.TotalSeconds / _engine.TotalTime.TotalSeconds * 100;
        PlaybackTimeText.Text = $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";

        // Crossfade trigger: start next track early so they overlap.
        if (_crossfadeEnabled && _autoplay && !_crossfadeTriggered
            && _nextTrackIndex >= 0 && _engine.State == EngineState.Playing)
        {
            var total = _engine.TotalTime.TotalSeconds;
            var current = _engine.CurrentTime.TotalSeconds;
            // Only trigger if the song is long enough (prevents instant loop on short tracks).
            if (total >= _crossfadeDurationSeconds + 2.0 && current >= 1.0)
            {
                var remaining = total - current;
                if (remaining > 0 && remaining <= _crossfadeDurationSeconds)
                {
                    _crossfadeTriggered = true;
                    PlayTrackAt(_nextTrackIndex, isCrossfade: true);
                    return; // PlayTrackAt already updates upcoming bar
                }
            }
        }

        UpdateUpcomingBar();
    }

    // --- Navigation ---

    private int GetCurrentPlayIndex() =>
        _engine.ActiveTrackId < 0 ? -1 : _filteredItems.FindIndex(i => i.Track.Id == _engine.ActiveTrackId);

    private int PeekNextTrackIndex(int currentFilteredIndex)
    {
        if (_filteredItems.Count == 0) return -1;
        if (_repeatMode == RepeatMode.RepeatOne) return currentFilteredIndex;
        if (_shuffle)
        {
            if (_filteredItems.Count == 1) return 0;
            int next;
            do { next = _rng.Next(_filteredItems.Count); } while (next == currentFilteredIndex);
            return next;
        }
        if (_repeatMode == RepeatMode.RepeatAll)
            return (currentFilteredIndex + 1) % _filteredItems.Count;
        int nextIdx = currentFilteredIndex + 1;
        return nextIdx < _filteredItems.Count ? nextIdx : -1;
    }

    private void RefreshNextTrackPreview()
    {
        if (_engine.ActiveTrackId < 0) { _nextTrackIndex = -1; UpdateUpcomingBar(); return; }
        var currentIdx = GetCurrentPlayIndex();
        if (currentIdx < 0) { _nextTrackIndex = -1; UpdateUpcomingBar(); return; }
        _nextTrackIndex = PeekNextTrackIndex(currentIdx);
        UpdateUpcomingBar();
    }

    private void NavigateNext(bool isManual)
    {
        if (_filteredItems.Count == 0) { FullStop(); return; }

        var currentIdx = GetCurrentPlayIndex();

        if (_repeatMode == RepeatMode.RepeatOne && !isManual)
        {
            PlayTrackAt(currentIdx >= 0 ? currentIdx : 0, isCrossfade: false);
            return;
        }

        if (_shuffle)
        {
            int nextIdx;
            if (_filteredItems.Count == 1) { nextIdx = 0; }
            else { do { nextIdx = _rng.Next(_filteredItems.Count); } while (nextIdx == currentIdx); }
            PlayTrackAt(nextIdx, isCrossfade: false);
            return;
        }

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
                if (nextLinearIdx >= _filteredItems.Count) { FullStop(); return; }
            }
            else if (_engine.ActiveTrackId < 0)
            {
                var selIdx = FileList.SelectedIndex;
                nextLinearIdx = selIdx >= 0 && selIdx < _filteredItems.Count ? selIdx : 0;
            }
            else
            {
                nextLinearIdx = 0;
            }
        }

        PlayTrackAt(nextLinearIdx, isCrossfade: false);
    }

    private void NavigatePrevious()
    {
        if (_filteredItems.Count == 0) return;

        if (_shuffle && _shuffleHistory.Count > 0)
        {
            while (_shuffleHistory.Count > 0)
            {
                var histId = _shuffleHistory[^1];
                _shuffleHistory.RemoveAt(_shuffleHistory.Count - 1);
                var histIdx = _filteredItems.FindIndex(i => i.Track.Id == histId);
                if (histIdx >= 0) { PlayTrackAt(histIdx, isCrossfade: false); return; }
            }
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
                var selIdx = FileList.SelectedIndex;
                if (selIdx <= 0) return;
                prevIdx = selIdx - 1;
            }
            else if (currentIdx == 0) { return; }
            else { prevIdx = currentIdx - 1; }
        }

        PlayTrackAt(prevIdx, isCrossfade: false);
    }

    private void FullStop()
    {
        _engine.Stop();
        // OnEngineStateChanged will handle UI reset.
    }

    public void StopPlayback()
    {
        _engine.Stop();
    }

    // --- Upcoming bar ---

    private void UpdateUpcomingBar()
    {
        if (!_showUpcomingTrackBar || _engine.State == EngineState.Stopped
            || _nextTrackIndex < 0 || _nextTrackIndex >= _filteredItems.Count)
        {
            UpcomingBar.IsVisible = false;
            return;
        }

        UpcomingBar.IsVisible = true;
        UpcomingTrackText.Text = _filteredItems[_nextTrackIndex].Track.Title;

        if (_engine.IsCrossfading)
        {
            CrossfadeStatusText.Text = "↗ crossfading";
        }
        else if (_crossfadeEnabled && _engine.TotalTime.TotalSeconds > 0)
        {
            var remaining = (_engine.TotalTime - _engine.CurrentTime).TotalSeconds;
            CrossfadeStatusText.Text = remaining > 0 ? $"xfade in {(int)remaining}s" : "";
        }
        else
        {
            CrossfadeStatusText.Text = "";
        }
    }

    // --- Progress / seeking ---

    private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_engine.State == EngineState.Stopped) return;
        _isSeeking = true;
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_engine.State == EngineState.Stopped) { _isSeeking = false; return; }
        _engine.Seek(PlaybackSlider.Value / 100.0);
        PlaybackTimeText.Text = $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";
        _isSeeking = false;
    }

    // --- UI helpers ---

    private void UpdateButtonStates()
    {
        PauseResumeBtn.IsVisible = _engine.State != EngineState.Stopped;
        PauseResumeBtn.Content = _engine.State == EngineState.Paused ? "Resume" : "Pause";
    }

    private void ResetPlaybackUI()
    {
        NowPlayingText.Text = "";
        PlaybackSlider.Value = 0;
        PlaybackTimeText.Text = "";
        PlaybackInfoPanel.IsVisible = false;
    }

    private void RefreshPlayingMarkers()
    {
        if (_filteredItems.Count == 0) return;

        var selectedId = FileList.SelectedIndex >= 0 && FileList.SelectedIndex < _filteredItems.Count
            ? _filteredItems[FileList.SelectedIndex].Track.Id : -1;

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

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
