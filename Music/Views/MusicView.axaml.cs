using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class MusicView : UserControl
{
    private enum RepeatMode { None, RepeatOne, RepeatAll }

    // Engine
    private readonly PlaybackEngine _engine = new();
    private readonly PlaybackSession _session = new();
    private bool _isSeeking;

    // Playback settings
    private int _crossfadeDurationSeconds = 10;
    private int _manualFadeDurationSeconds = 2;
    private bool _showUpcomingTrackBar = true;
    private RepeatMode _repeatMode = RepeatMode.None;
    private bool _shuffle;
    private bool _loadingSettings;

    // UI state
    private bool _filterPanelVisible;
    private CancellationTokenSource? _thumbLoadCts;

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private int _nextTrackIndex = -1;

    // Shuffle history
    private readonly List<int> _shuffleHistory = [];
    private readonly Random _rng = new();

    // Track list data
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private List<TrackDisplayItem> _filteredItems = [];

    private record FilterGroupControls(
        MultiSelectFilterControl GenreCtrl,
        MultiSelectFilterControl StyleCtrl);
    private readonly List<FilterGroupControls> _filterGroups = [];

    public MusicView()
    {
        InitializeComponent();

        // Engine events
        _engine.StateChanged += OnEngineStateChanged;
        _engine.TrackNaturallyEnded += OnTrackNaturallyEnded;
        _engine.ProgressUpdated += OnProgressUpdated;

        // Seeking
        PlaybackSlider.AddHandler(PointerPressedEvent,
            OnSliderPointerPressed, RoutingStrategies.Tunnel);
        PlaybackSlider.AddHandler(PointerReleasedEvent,
            OnSliderPointerReleased, RoutingStrategies.Tunnel);

        SearchBox.TextChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        ReEvalFilterCheckBox.IsCheckedChanged += (_, _) => ApplyFilter();

        // Volume
        VolumeSlider.ValueChanged += (_, _) =>
        {
            Values.Volume = (float)VolumeSlider.Value / 100.0f;
            _engine.ApplyVolume();
        };
        
        try { MusicLibraryService.Current.Initialize(); }
        catch (Exception ex) { StatusText.Text = $"Database error: {ex.Message}"; StatusText.IsVisible = true; return; }

        LoadLookups();
        AddFilterGroup();
        RefreshTrackList();

        AddTrackOverlay.TrackDownloaded += () => { AddTrackOverlay.IsVisible = false; RefreshTrackList(); };
        AddTrackOverlay.CloseRequested += () => AddTrackOverlay.IsVisible = false;
    }

    // ─── Track list ──────────────────────────────────────────────────────────

    private void LoadLookups()
    {
        Values.Genres = MusicLibraryService.Current.GetGenres();
        Values.Styles = MusicLibraryService.Current.GetStyles();
        Values.Ratings = MusicLibraryService.Current.GetRatings();

        RatingFilter.SetItems(Values.Ratings.Select(r => r.Name));

        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.SetItems(Values.Genres.Select(g => g.Name));
            fg.StyleCtrl.SetItems(Values.Styles.Select(s => s.Name));
        }
    }

    private void RefreshTrackList()
    {
        _thumbLoadCts?.Cancel();
        _thumbLoadCts = new CancellationTokenSource();

        foreach (var item in _allItems)
            item.Thumbnail?.Dispose();

        var tracks = MusicLibraryService.Current.GetTracks();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();

        var genreMap = Values.Genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = Values.Ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = Values.Styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var genreIds = _allTrackGenreIds.GetValueOrDefault(t.Id, []);
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);

            var genreStr = string.Join(", ", genreIds
                .Select(id => genreMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var styleStr = string.Join(", ", styleIds
                .Select(id => styleMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var ratingName = ratingMap.GetValueOrDefault(t.RatingId, "");
            var durationText = t.DurationSeconds.HasValue ? FormatDuration(t.DurationSeconds.Value) : "";

            var miscParts = new List<string>();
            if (t.ReEvaluationNeeded) miscParts.Add("[re-eval]");

            return new TrackDisplayItem(t, string.Join(" · ", miscParts),
                genreIds, styleIds,
                genreStr, styleStr, durationText, ratingName);
        }).ToList();

        ApplyFilter();
        _ = LoadThumbnailsAsync(_thumbLoadCts.Token);
    }

    private async Task LoadThumbnailsAsync(CancellationToken ct)
    {
        var items = _allItems.ToList();

        // Extract embedded artwork to disk cache on a background thread
        Dictionary<int, string?> paths;
        try
        {
            paths = await Task.Run(() =>
            {
                var result = new Dictionary<int, string?>();
                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    result[item.Track.Id] = MusicLibraryService.Current.EnsureThumbnailCached(
                        item.Track.Id, item.Track.FileName);
                }
                return result;
            }, ct);
        }
        catch (OperationCanceledException) { return; }

        if (ct.IsCancellationRequested) return;

        // Load Bitmaps on UI thread (reading small cached JPEGs is fast)
        bool any = false;
        foreach (var item in items)
        {
            if (paths.TryGetValue(item.Track.Id, out var path) && path != null)
            {
                try { item.Thumbnail = new Bitmap(path); any = true; }
                catch { }
            }
        }

        if (!any || ct.IsCancellationRequested) return;

        // Re-assign ItemsSource to trigger a re-render with thumbnails
        var sel = FileList.SelectedIndex;
        FileList.ItemsSource = _filteredItems.ToList();
        if (sel >= 0 && sel < _filteredItems.Count)
            FileList.SelectedIndex = sel;
    }

    private void ApplyFilter()
    {
        var selRatingIds = SelectedIds(RatingFilter.SelectedItems, Values.Ratings, r => r.Name, r => r.Id);
        var ratingSortOrders = Values.Ratings.ToDictionary(r => r.Id, r => r.SortOrder);
        var itemById = _allItems.ToDictionary(i => i.Track.Id);

        var groups = _filterGroups
            .Select(fg => new FilterGroup(
                SelectedIds(fg.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id),
                SelectedIds(fg.StyleCtrl.SelectedItems, Values.Styles, s => s.Name, s => s.Id)))
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
            SearchBox.Text);

        _filteredItems = filtered
            .Where(t => itemById.ContainsKey(t.Id))
            .Select(t => itemById[t.Id])
            .ToList();

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        FileList.ItemsSource = _filteredItems;
        RefreshNextTrackPreview();
        UpdateFilterChips();
        UpdateFilterCounts();
    }

    private void UpdateFilterCounts()
    {
        var currentTrackIds = _filteredItems.Select(i => i.Track.Id).ToList();

        var genreFacetCounts = MetadataCountService.FacetCounts(currentTrackIds, _allTrackGenreIds);
        var styleFacetCounts = MetadataCountService.FacetCounts(currentTrackIds, _allTrackStyleIds);

        var genreCountByName = Values.Genres.ToDictionary(g => g.Name,
            g => genreFacetCounts.GetValueOrDefault(g.Id, 0));
        var styleCountByName = Values.Styles.ToDictionary(s => s.Name,
            s => styleFacetCounts.GetValueOrDefault(s.Id, 0));

        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.UpdateCounts(genreCountByName);
            fg.StyleCtrl.UpdateCounts(styleCountByName);
        }
    }

    // ─── Session helpers ──────────────────────────────────────────────────────

    private void CloseCurrentSession()
    {
        if (!_session.HasSession) return;
        _session.Flush();
        _session.Reset();
    }

    private void UpdateFilterChips()
    {
        var chips = new List<string>();

        foreach (var r in Enumerable.OrderBy<string, string>(RatingFilter.SelectedItems, n => n))
            chips.Add($"Rating: {r}");

        if (ReEvalFilterCheckBox.IsChecked == true)
            chips.Add("Re-eval");

        var seenGenres = new HashSet<string>();
        var seenStyles = new HashSet<string>();
        var seenLangs = new HashSet<string>();

        foreach (var fg in _filterGroups)
        {
            foreach (var g in fg.GenreCtrl.SelectedItems.OrderBy(n => n))
                if (seenGenres.Add(g)) chips.Add($"Genre: {g}");
            foreach (var s in fg.StyleCtrl.SelectedItems.OrderBy(n => n))
                if (seenStyles.Add(s)) chips.Add($"Style: {s}");
        }

        ActiveFilterChips.ItemsSource = chips.Count > 0 ? (IEnumerable<string>)chips : null;
        ActiveFilterChips.IsVisible = chips.Count > 0;
    }

    private static HashSet<int> SelectedIds<T>(IReadOnlySet<string> selected, List<T> source,
        Func<T, string> nameOf, Func<T, int> idOf)
    {
        if (selected.Count == 0) return [];
        return source.Where(item => selected.Contains(nameOf(item))).Select(idOf).ToHashSet();
    }

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

    // ─── Toolbar / filter panel ───────────────────────────────────────────────

    private void OnToggleFiltersClicked(object? sender, RoutedEventArgs e)
    {
        _filterPanelVisible = !_filterPanelVisible;
        FilterDrawer.IsVisible = _filterPanelVisible;
    }

    private void OnClearFiltersClicked(object? sender, RoutedEventArgs e)
    {
        RatingFilter.SetItems(Values.Ratings.Select(r => r.Name));
        RatingFilter.Placeholder = "All ratings";
        ReEvalFilterCheckBox.IsChecked = false;
        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.SetItems(Values.Genres.Select(g => g.Name));
            fg.StyleCtrl.SetItems(Values.Styles.Select(s => s.Name));
        }
        ApplyFilter();
    }

    // ─── Filter groups ────────────────────────────────────────────────────────

    private void OnAddFilterGroupClicked(object? sender, RoutedEventArgs e)
    {
        AddFilterGroup();
        ApplyFilter();
    }

    private void AddFilterGroup()
    {
        var genreCtrl = new MultiSelectFilterControl { Placeholder = "All genres" };
        genreCtrl.SetItems(Values.Genres.Select(g => g.Name));
        genreCtrl.SelectionChanged += (_, _) => ApplyFilter();

        var styleCtrl = new MultiSelectFilterControl { Placeholder = "All styles" };
        styleCtrl.SetItems(Values.Styles.Select(s => s.Name));
        styleCtrl.SelectionChanged += (_, _) => ApplyFilter();

        var fg = new FilterGroupControls(genreCtrl, styleCtrl);
        _filterGroups.Add(fg);

        StackPanel Section(string label, MultiSelectFilterControl ctrl) =>
            new StackPanel { Spacing = 5, Margin = new Thickness(0, 0, 0, 10),
                Children = {
                    new TextBlock { Text = label, FontSize = 11, Opacity = 0.5 },
                    ctrl
                }
            };

        var body = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        body.Children.Add(Section("Genre", genreCtrl));
        body.Children.Add(Section("Style", styleCtrl));

        var card = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };

        // Only groups after the first get a divider + remove button
        if (_filterGroups.Count > 1)
        {
            var header = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var groupLabel = new TextBlock
            {
                Text = $"Group {_filterGroups.Count}",
                FontSize = 11, Opacity = 0.4,
                VerticalAlignment = VerticalAlignment.Center
            };
            var removeBtn = new Button
            {
                Content = "×", Padding = new Thickness(6, 2),
                FontSize = 12, Opacity = 0.5,
                Background = new SolidColorBrush(Colors.Transparent)
            };
            removeBtn.Click += (_, _) => RemoveFilterGroup(fg);

            Grid.SetColumn(groupLabel, 0);
            Grid.SetColumn(removeBtn, 1);
            header.Children.Add(groupLabel);
            header.Children.Add(removeBtn);

            var divider = new Border
            {
                Height = 1, Margin = new Thickness(0, 0, 0, 4),
                Background = new SolidColorBrush(Color.FromArgb(22, 128, 128, 128))
            };
            card.Children.Add(divider);
            card.Children.Add(header);
        }

        card.Children.Add(body);
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

    // ─── Dialogs ──────────────────────────────────────────────────────────────

    private void OnAddTrackClicked(object? sender, RoutedEventArgs e)
    {
        AddTrackOverlay.Open();
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

    // ─── Playback control ─────────────────────────────────────────────────────

    private void OnListDoubleTapped(object? sender, TappedEventArgs e) => StartPlayback();
    private void OnPreviousClicked(object? sender, RoutedEventArgs e) => NavigatePrevious();
    private void OnNextClicked(object? sender, RoutedEventArgs e) => NavigateNext(isManual: true);

    private void OnPlayPauseClicked(object? sender, RoutedEventArgs e)
    {
        if (_engine.State == EngineState.Playing)
        {
            _engine.Pause();
            UpdateButtonStates();
        }
        else if (_engine.State == EngineState.Paused)
        {
            _engine.Resume();
            UpdateButtonStates();
        }
        else
        {
            StartPlayback();
        }
    }

    private void OnRepeatCycleClicked(object? sender, RoutedEventArgs e)
    {
        _repeatMode = _repeatMode switch
        {
            RepeatMode.None => RepeatMode.RepeatAll,
            RepeatMode.RepeatAll => RepeatMode.RepeatOne,
            _ => RepeatMode.None,
        };
    }

    private void OnShuffleToggleClicked(object? sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        if (!_shuffle) _shuffleHistory.Clear();
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

        // Close any open session (manual track change). Natural-end sessions are closed
        // in OnTrackNaturallyEnded before this point, so this handles the skip case.
        CloseCurrentSession();

        var track = _filteredItems[filteredIndex].Track;
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);

        bool wasPlaying = _engine.State != EngineState.Stopped;
        float fadeOut = isCrossfade ? _crossfadeDurationSeconds
                      : wasPlaying ? _manualFadeDurationSeconds : 0f;
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
            StatusText.IsVisible = true;
            return;
        }

        _session.Start(track.Id, track.DurationSeconds ?? 0);

        FileList.SelectedIndex = filteredIndex;

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

    // ─── Engine events ────────────────────────────────────────────────────────

    private void OnEngineStateChanged()
    {
        // Keep session wall-clock accumulation accurate across pause/resume.
        if (_engine.State == EngineState.Paused)
            _session.OnPause();
        else if (_engine.State == EngineState.Playing && _session.HasSession)
            _session.OnResume();

        UpdateButtonStates();
        if (_engine.State == EngineState.Stopped)
        {
            _nextTrackIndex = -1;
            _crossfadeTriggered = false;
            _lastKnownActiveId = -1;
            RefreshPlayingMarkers();
            UpdateUpcomingBar();
        }
    }

    private void OnTrackNaturallyEnded()
    {
        // Close session before navigating so the new PlayTrackAt call doesn't see a stale session.
        CloseCurrentSession();
        NavigateNext(isManual: false);
    }

    private void OnProgressUpdated()
    {
        if (_engine.ActiveTrackId != _lastKnownActiveId)
        {
            _lastKnownActiveId = _engine.ActiveTrackId;
            _crossfadeTriggered = false;
        }

        // Track position for listen-threshold detection; the count is committed on session close.
        if (_session.HasSession)
            _session.OnProgress(_engine.CurrentTime.TotalSeconds);

        if (!_isSeeking && _engine.TotalTime.TotalSeconds > 0)
            PlaybackSlider.Value =
                _engine.CurrentTime.TotalSeconds / _engine.TotalTime.TotalSeconds * 100;

        PlaybackTimeText.Text =
            $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";

        if (!_crossfadeTriggered && _nextTrackIndex >= 0 && _engine.State == EngineState.Playing)
        {
            var total = _engine.TotalTime.TotalSeconds;
            var current = _engine.CurrentTime.TotalSeconds;
            if (total >= _crossfadeDurationSeconds + 2.0 && current >= 1.0)
            {
                var remaining = total - current;
                if (remaining > 0 && remaining <= _crossfadeDurationSeconds)
                {
                    _crossfadeTriggered = true;
                    PlayTrackAt(_nextTrackIndex, isCrossfade: true);
                    return;
                }
            }
        }

        UpdateUpcomingBar();
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    private int GetCurrentPlayIndex() =>
        _engine.ActiveTrackId < 0
            ? -1
            : _filteredItems.FindIndex(i => i.Track.Id == _engine.ActiveTrackId);

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
        CloseCurrentSession();
        _engine.Stop();
    }

    // ─── Upcoming bar ─────────────────────────────────────────────────────────

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
        else if (_engine.TotalTime.TotalSeconds > 0)
        {
            var remaining = (_engine.TotalTime - _engine.CurrentTime).TotalSeconds;
            CrossfadeStatusText.Text = remaining > 0 ? $"xfade in {(int)remaining}s" : "";
        }
        else
        {
            CrossfadeStatusText.Text = "";
        }
    }

    // ─── Progress / seeking ───────────────────────────────────────────────────

    private void OnSliderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_engine.State == EngineState.Stopped) return;
        _isSeeking = true;
    }

    private void OnSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_engine.State == EngineState.Stopped) { _isSeeking = false; return; }
        _engine.Seek(PlaybackSlider.Value / 100.0);
        PlaybackTimeText.Text =
            $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";
        _isSeeking = false;
    }

    // ─── UI helpers ───────────────────────────────────────────────────────────

    private void UpdateButtonStates()
    {
        PlayPauseBtn.Content = _engine.State == EngineState.Playing ? "⏸" : "▶";
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

    // ─── Formatting ───────────────────────────────────────────────────────────

    private static string FormatDuration(TimeSpan t) => FormatDuration((int)t.TotalSeconds);

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m:D2}:{s:D2}";
    }
}
