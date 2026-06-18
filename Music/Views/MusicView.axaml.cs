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
using Avalonia.Platform.Storage;
using Music.Core;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class MusicView : UserControl
{
    // Engine
    private readonly PlaybackEngine _engine = new();
    private bool _isSeeking;

    // Playback settings
    private bool _shuffle;

    // UI state
    private bool _filterPanelVisible;
    private CancellationTokenSource? _thumbLoadCts;

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private int _nextTrackIndex = -1;

    private readonly Random _rng = new();

    // Track list data
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private List<TrackDisplayItem> _filteredItems = [];
    private List<PortableFilterPreset> _filterPresets = [];
    private bool _updatingPresetUi;

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

        // Volume
        VolumeSlider.ValueChanged += (_, _) =>
        {
            Values.Volume = (float)VolumeSlider.Value / 100.0f;
            _engine.ApplyVolume(Values.Volume);
        };
        
        try { MusicLibraryService.Current.Initialize(); }
        catch (Exception ex) { StatusText.Text = $"Database error: {ex.Message}"; StatusText.IsVisible = true; return; }

        LoadLookups();
        LoadFilterPresets();
        AddFilterGroup();
        RefreshTrackList();

        AddTrackOverlay.TrackDownloaded += () => { AddTrackOverlay.IsVisible = false; RefreshTrackList(); };
        AddTrackOverlay.CloseRequested += () => AddTrackOverlay.IsVisible = false;
        EditTrackOverlay.TrackSaved += RefreshTrackList;
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
            
            return new TrackDisplayItem(t, genreStr, styleStr, durationText, ratingName);
        }).ToList();

        ApplyFilter();
        _ = LoadThumbnailsAsync(_thumbLoadCts.Token);
    }

    private async Task LoadThumbnailsAsync(CancellationToken ct)
    {
        var items = _allItems.ToList();

        Dictionary<int, byte[]?> artworkByTrackId;
        try
        {
            artworkByTrackId = await Task.Run(() =>
            {
                var result = new Dictionary<int, byte[]?>();
                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    var audioFilePath = Path.Combine(Values.TracksDirectory, item.Track.FileName);
                    result[item.Track.Id] = ThumbnailService.ReadEmbeddedArtwork(audioFilePath);
                }
                return result;
            }, ct);
        }
        catch (OperationCanceledException) { return; }

        if (ct.IsCancellationRequested) return;

        bool any = false;
        foreach (var item in items)
        {
            if (artworkByTrackId.TryGetValue(item.Track.Id, out var artwork) && artwork != null)
            {
                try
                {
                    using var stream = new MemoryStream(artwork);
                    item.Thumbnail = new Bitmap(stream);
                    any = true;
                }
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
        var itemById = _allItems.ToDictionary(i => i.Track.Id);

        var groups = _filterGroups
            .Select(fg => new FilterGroup(
                SelectedIds(fg.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id),
                SelectedIds(fg.StyleCtrl.SelectedItems, Values.Styles, s => s.Name, s => s.Id)))
            .ToList();
        
        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackGenreIds,
            _allTrackStyleIds,
            selRatingIds,
            groups,
            SearchBox.Text);

        _filteredItems = filtered
            .Where(t => itemById.ContainsKey(t.Id))
            .Select(t => itemById[t.Id])
            .ToList();

        if (_shuffle)
            ShuffleFilteredItems();

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        FileList.ItemsSource = _filteredItems;
        RefreshNextTrackPreview();
        UpdateFilterCounts();
    }

    private void UpdateFilterCounts()
    {
        foreach (var fg in _filterGroups)
        {
            var groupTracks = TracksMatchingSearchRatingAndGroup(fg);
            var groupTrackIds = groupTracks.Select(track => track.Id).ToList();

            var genreFacetCounts = MetadataCountService.FacetCounts(groupTrackIds, _allTrackGenreIds);
            var styleFacetCounts = MetadataCountService.FacetCounts(groupTrackIds, _allTrackStyleIds);

            var genreCountByName = Values.Genres.ToDictionary(g => g.Name,
                g => genreFacetCounts.GetValueOrDefault(g.Id, 0));
            var styleCountByName = Values.Styles.ToDictionary(s => s.Name,
                s => styleFacetCounts.GetValueOrDefault(s.Id, 0));

            fg.GenreCtrl.UpdateCounts(genreCountByName);
            fg.StyleCtrl.UpdateCounts(styleCountByName);
        }
    }

    private List<MusicTrack> TracksMatchingSearchRatingAndGroup(FilterGroupControls group)
    {
        IEnumerable<MusicTrack> query = _allItems.Select(item => item.Track);
        var selectedRatingIds = SelectedIds(RatingFilter.SelectedItems, Values.Ratings, r => r.Name, r => r.Id);
        var selectedGenreIds = SelectedIds(group.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id);
        var selectedStyleIds = SelectedIds(group.StyleCtrl.SelectedItems, Values.Styles, s => s.Name, s => s.Id);
        var term = SearchBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(track => track.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (selectedRatingIds.Count > 0)
            query = query.Where(track => selectedRatingIds.Contains(track.RatingId));

        if (selectedGenreIds.Count > 0)
            query = query.Where(track => TrackHasAllTags(track.Id, _allTrackGenreIds, selectedGenreIds));

        if (selectedStyleIds.Count > 0)
            query = query.Where(track => TrackHasAllTags(track.Id, _allTrackStyleIds, selectedStyleIds));

        return query.ToList();
    }

    private static bool TrackHasAllTags(
        int trackId,
        IReadOnlyDictionary<int, List<int>> trackTagIds,
        IReadOnlySet<int> selectedTagIds)
    {
        trackTagIds.TryGetValue(trackId, out var trackTags);
        trackTags ??= [];
        return selectedTagIds.All(trackTags.Contains);
    }

    private static HashSet<int> SelectedIds<T>(IReadOnlySet<string> selected, List<T> source,
        Func<T, string> nameOf, Func<T, int> idOf)
    {
        if (selected.Count == 0) return [];
        return source.Where(item => selected.Contains(nameOf(item))).Select(idOf).ToHashSet();
    }

    // ─── Toolbar / filter panel ───────────────────────────────────────────────

    private void OnToggleFiltersClicked(object? sender, RoutedEventArgs e)
    {
        _filterPanelVisible = !_filterPanelVisible;
        FilterDrawer.IsVisible = _filterPanelVisible;
    }

    private void OnClearFiltersClicked(object? sender, RoutedEventArgs e)
    {
        _updatingPresetUi = true;
        PresetBox.SelectedIndex = -1;
        PresetNameBox.Text = "";
        _updatingPresetUi = false;

        RatingFilter.SetItems(Values.Ratings.Select(r => r.Name));
        RatingFilter.Placeholder = "All ratings";
        _filterGroups.Clear();
        FilterGroupsPanel.Children.Clear();
        AddFilterGroup();
        ApplyFilter();
    }

    private void LoadFilterPresets()
    {
        _filterPresets = FilterPresetStore.Load();
        RefreshPresetBox();
    }

    private void RefreshPresetBox(string? selectedName = null)
    {
        _updatingPresetUi = true;

        var names = _filterPresets
            .Select(preset => preset.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        PresetBox.ItemsSource = names;
        PresetBox.SelectedItem = selectedName != null && names.Contains(selectedName, StringComparer.OrdinalIgnoreCase)
            ? names.First(name => string.Equals(name, selectedName, StringComparison.OrdinalIgnoreCase))
            : null;

        _updatingPresetUi = false;
    }

    private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingPresetUi || PresetBox.SelectedItem is not string presetName)
            return;

        var preset = _filterPresets.FirstOrDefault(p =>
            string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));

        if (preset is null)
            return;

        PresetNameBox.Text = preset.Name;
        ApplyFilterPreset(preset);
    }

    private void OnSavePresetClicked(object? sender, RoutedEventArgs e)
    {
        var name = PresetNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) && PresetBox.SelectedItem is string selectedName)
            name = selectedName;

        if (string.IsNullOrWhiteSpace(name))
            return;

        var preset = CreatePreset(name);
        var index = _filterPresets.FindIndex(existing =>
            string.Equals(existing.Name, preset.Name, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
            _filterPresets[index] = preset;
        else
            _filterPresets.Add(preset);

        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();
        PresetNameBox.Text = preset.Name;
        RefreshPresetBox(preset.Name);
    }

    private void OnDeletePresetClicked(object? sender, RoutedEventArgs e)
    {
        var name = PresetBox.SelectedItem as string ?? PresetNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        _filterPresets.RemoveAll(preset =>
            string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));

        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();
        PresetNameBox.Text = "";
        RefreshPresetBox();
    }

    private PortableFilterPreset CreatePreset(string name)
    {
        var groups = _filterGroups
            .Select(group => new PortableFilterGroup(
                SortedNames(group.GenreCtrl.SelectedItems),
                SortedNames(group.StyleCtrl.SelectedItems)))
            .Where(group => group.Genres.Count > 0 || group.Styles.Count > 0)
            .ToList();

        return new PortableFilterPreset(
            name,
            SortedNames(RatingFilter.SelectedItems),
            groups);
    }

    private void ApplyFilterPreset(PortableFilterPreset preset)
    {
        RatingFilter.SetSelectedItems(preset.Ratings, notify: false);

        _filterGroups.Clear();
        FilterGroupsPanel.Children.Clear();

        var groups = preset.Groups
            .Where(group => group.Genres.Count > 0 || group.Styles.Count > 0)
            .ToList();

        if (groups.Count == 0)
        {
            AddFilterGroup();
        }
        else
        {
            foreach (var group in groups)
            {
                var controls = AddFilterGroup();
                controls.GenreCtrl.SetSelectedItems(group.Genres, notify: false);
                controls.StyleCtrl.SetSelectedItems(group.Styles, notify: false);
            }
        }

        ApplyFilter();
    }

    private static List<string> SortedNames(IEnumerable<string> names) =>
        names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

    // ─── Filter groups ────────────────────────────────────────────────────────

    private void OnAddFilterGroupClicked(object? sender, RoutedEventArgs e)
    {
        AddFilterGroup();
        ApplyFilter();
    }

    private FilterGroupControls AddFilterGroup()
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
                Background = new SolidColorBrush(Color.FromArgb(70, 49, 75, 95))
            };
            card.Children.Add(divider);
            card.Children.Add(header);
        }

        card.Children.Add(body);
        FilterGroupsPanel.Children.Add(card);
        return fg;
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

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Export Android library",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;

        try
        {
            await MusicLibraryService.Current.ExportPortableLibraryAsync(folders[0].Path.LocalPath);
            StatusText.Text = "";
            StatusText.IsVisible = false;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
            StatusText.IsVisible = true;
        }
    }

    private void OnContextEditClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count) return;
        EditTrackOverlay.Open(_filteredItems[idx].Track);
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

    private void OnShuffleToggleClicked(object? sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        ApplyFilter();
        FileList.SelectedIndex = _filteredItems.Count > 0 ? 0 : -1;
        ShuffleBtn.Opacity = _shuffle ? 1.0 : 0.35;
        ToolTip.SetTip(ShuffleBtn, _shuffle ? "Shuffle: On" : "Shuffle: Off");
        if (_filteredItems.Count > 0)
            PlayTrackAt(0, isCrossfade: false);
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
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);

        bool wasPlaying = _engine.State != EngineState.Stopped;
        float fadeOut = isCrossfade ? Values.CrossfadeDurationSeconds
                      : wasPlaying ? Values.ManualFadeDurationSeconds : 0f;
        float fadeIn = isCrossfade ? Values.CrossfadeDurationSeconds
                     : wasPlaying ? Values.ManualFadeDurationSeconds : 0f;

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

        FileList.SelectedIndex = filteredIndex;

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
        NavigateNext(isManual: false);
    }

    private void OnProgressUpdated()
    {
        if (_engine.ActiveTrackId != _lastKnownActiveId)
        {
            _lastKnownActiveId = _engine.ActiveTrackId;
            _crossfadeTriggered = false;
        }

        if (!_isSeeking && _engine.TotalTime.TotalSeconds > 0)
            PlaybackSlider.Value =
                _engine.CurrentTime.TotalSeconds / _engine.TotalTime.TotalSeconds * 100;

        PlaybackTimeText.Text =
            $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";

        if (!_crossfadeTriggered && _nextTrackIndex >= 0 && _engine.State == EngineState.Playing)
        {
            var total = _engine.TotalTime.TotalSeconds;
            var current = _engine.CurrentTime.TotalSeconds;
            if (total >= Values.CrossfadeDurationSeconds + 2.0 && current >= 1.0)
            {
                var remaining = total - current;
                if (remaining > 0 && remaining <= Values.CrossfadeDurationSeconds)
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

        int nextLinearIdx;
        if (currentIdx >= 0)
        {
            nextLinearIdx = currentIdx + 1;
            if (nextLinearIdx >= _filteredItems.Count) { FullStop(); return; }
        }
        else if (_engine.ActiveTrackId < 0)
        {
            var selIdx = FileList.SelectedIndex;
            nextLinearIdx = selIdx >= 0 && selIdx < _filteredItems.Count
                ? selIdx + (isManual ? 0 : 1)
                : 0;
            if (nextLinearIdx >= _filteredItems.Count) { FullStop(); return; }
        }
        else
        {
            nextLinearIdx = 0;
        }

        PlayTrackAt(nextLinearIdx, isCrossfade: false);
    }

    private void NavigatePrevious()
    {
        if (_filteredItems.Count == 0) return;

        var currentIdx = GetCurrentPlayIndex();
        int prevIdx;
        if (currentIdx < 0)
        {
            var selIdx = FileList.SelectedIndex;
            if (selIdx <= 0) return;
            prevIdx = selIdx - 1;
        }
        else if (currentIdx == 0) { return; }
        else { prevIdx = currentIdx - 1; }

        PlayTrackAt(prevIdx, isCrossfade: false);
    }

    private void FullStop()
    {
        _engine.Stop();
    }

    // ─── Upcoming bar ─────────────────────────────────────────────────────────

    private void UpdateUpcomingBar()
    {
        if (_engine.State == EngineState.Stopped
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
        var isPlaying = _engine.State == EngineState.Playing;
        PlayIcon.IsVisible = !isPlaying;
        PauseIcon.IsVisible = isPlaying;
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

    private void ShuffleFilteredItems()
    {
        for (var i = _filteredItems.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (_filteredItems[i], _filteredItems[j]) = (_filteredItems[j], _filteredItems[i]);
        }
    }

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m:D2}:{s:D2}";
    }
}
