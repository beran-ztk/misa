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
using Avalonia.VisualTree;
using Music.Core;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class MusicView : UserControl
{
    // Engine
    private readonly PlaybackEngine _engine = new();
    private readonly GlobalMediaKeyListener _globalMediaKeys = new();
    private readonly WindowsMediaSession _windowsMediaSession = new();
    private bool _isSeeking;

    // Playback settings
    private bool _shuffle;

    // UI state
    private bool _filterPanelVisible;
    private CancellationTokenSource? _thumbLoadCts;
    private CancellationTokenSource? _toastCts;

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private int _nextTrackIndex = -1;
    private int _listeningTrackId = -1;
    private double _lastListeningPositionSeconds;
    private double _unflushedListeningSeconds;

    private readonly Random _rng = new();

    // Track list data
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private List<TrackDisplayItem> _filteredItems = [];
    private List<PortableFilterPreset> _filterPresets = [];
    private bool _updatingPresetUi;
    private bool _showReviewOnly;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreCtrl,
        MultiSelectFilterControl StyleCtrl);
    private readonly List<FilterGroupControls> _filterGroups = [];

    public MusicView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _globalMediaKeys.Pressed += OnGlobalMediaKeyPressed;
        _globalMediaKeys.Start();
        _windowsMediaSession.Pressed += OnGlobalMediaKeyPressed;
        DetachedFromVisualTree += (_, _) =>
        {
            _globalMediaKeys.Dispose();
            _windowsMediaSession.Dispose();
        };

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
        FileList.SelectionChanged += (_, _) => UpdateReviewButton();
        PlayerBar.SizeChanged += (_, _) => UpdateSettingsLayout();
        PlayerBar.SizeChanged += (_, _) => UpdateEditorBounds();
        PlayerBar.SizeChanged += (_, _) => UpdateImportBounds();

        // Volume
        VolumeSlider.ValueChanged += (_, _) =>
        {
            Values.Volume = (float)VolumeSlider.Value / 100.0f;
            _engine.ApplyVolume(Values.Volume);
        };
        
        try { MusicLibraryService.Current.Initialize(); }
        catch (Exception ex) { StatusText.Text = $"Database error: {ex.Message}"; StatusText.IsVisible = true; return; }
        ImportQueueService.Current.Initialize();
        UpdateQueueStatus();
        UpdateImportBounds();

        LoadLookups();
        LoadFilterPresets();
        AddFilterGroup();
        RefreshTrackList();

        AddTrackOverlay.TrackDownloaded += warning =>
        {
            AddTrackOverlay.IsVisible = false;
            RefreshTrackList();
            ShowToast(warning ?? "Track downloaded and analyzed");
        };
        AddTrackOverlay.CloseRequested += () => AddTrackOverlay.IsVisible = false;
        ImportOverlay.QueueSubmitted += count =>
        {
            ShowToast($"{count} track{(count == 1 ? string.Empty : "s")} added to the import queue");
            UpdateQueueStatus();
        };
        ImportQueueService.Current.ItemUpdated += _ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            UpdateQueueStatus();
            if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
        });
        ImportQueueService.Current.TrackImported += (track, warning) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RefreshTrackList();
            ShowToast(warning ?? $"Imported: {track.Title}");
        });
        EditTrackOverlay.TrackSaved += RefreshTrackList;
        SettingsOverlay.ToastRequested += ShowToast;
        SettingsOverlay.LibraryMetadataChanged += RefreshLibraryPresentation;
        SettingsOverlay.TrackCalibrationRequested += track =>
        {
            SettingsOverlay.IsVisible = false;
            EditTrackOverlay.Open(track);
        };
    }

    private void UpdateEditorBounds()
    {
        // The editor covers the toolbar and library, but deliberately stops above the player.
        EditTrackOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
    }

    private void UpdateImportBounds() =>
        ImportOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);

    public void EnableSystemMediaControls()
    {
        _windowsMediaSession.Start();
        _windowsMediaSession.UpdateState(_engine.State);
    }

    private void RefreshLibraryPresentation()
    {
        LoadLookups();
        RefreshTrackList();
    }

    private void UpdateQueueStatus()
    {
        var summary = ImportQueueService.Current.GetSummary();
        var parts = new List<string>();
        if (summary.Downloading > 0) parts.Add("downloading");
        if (summary.Analyzing > 0) parts.Add("analyzing");
        if (summary.Queued > 0) parts.Add($"{summary.Queued} queued");
        QueueStatusText.IsVisible = parts.Count > 0;
        QueueStatusText.Text = parts.Count > 0 ? $"Queue · {string.Join(" · ", parts)}" : string.Empty;
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
        var systemGenreIds = MusicLibraryService.Current.GetAllTrackModelGenreIds();
        var manualGenreIds = MusicLibraryService.Current.GetAllTrackManualGenreIds();

        var genreMap = Values.Genres.ToDictionary(g => g.Id, g => g.Name);
        var ratingMap = Values.Ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = Values.Styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var genreIds = _allTrackGenreIds.GetValueOrDefault(t.Id, []);
            var systemIds = systemGenreIds.GetValueOrDefault(t.Id, []);
            var manualIds = manualGenreIds.GetValueOrDefault(t.Id, []);
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);

            var genreStr = string.Join(", ", systemIds
                .Select(id => genreMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var manualGenreStr = string.Join(", ", manualIds
                .Select(id => genreMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var styleStr = string.Join(", ", styleIds
                .Select(id => styleMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var ratingName = t.RatingId is int ratingId ? ratingMap.GetValueOrDefault(ratingId, "") : "Not rated";
            var durationText = t.DurationSeconds.HasValue ? FormatDuration(t.DurationSeconds.Value) : "";
            var attributes = MusicLibraryService.Current.GetTrackDerivedAttributes(t.Id);
            var profileText = string.Join(" · ", attributes
                .Where(attribute => attribute.Key is "emotional_tone" or "energy_context" or "intensity" or "vocal_presence")
                .Select(attribute => $"{ProfileAttributeName(attribute.Key)} {attribute.EffectiveValue}"));
            
            return new TrackDisplayItem(t, genreStr, manualGenreStr, styleStr, durationText, ratingName, profileText, t.ChannelName ?? "")
            {
                NeedsReview = t.NeedsReview
            };
        }).ToList();

        ApplyFilter();
        _ = LoadThumbnailsAsync(_thumbLoadCts.Token);
    }

    private static string ProfileAttributeName(string key) => key switch
    {
        "emotional_tone" => "Tone",
        "energy_context" => "Energy",
        "vocal_presence" => "Vocals",
        _ => char.ToUpperInvariant(key[0]) + key[1..]
    };

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

        if (_showReviewOnly)
            _filteredItems = _filteredItems
                .Where(item => item.NeedsReview)
                .ToList();

        if (_shuffle)
            ShuffleFilteredItems();

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        FileList.ItemsSource = _filteredItems;
        UpdatePlaylistSummary();
        RefreshNextTrackPreview();
        UpdateFilterCounts();
        UpdateReviewFilterButton();
        UpdateReviewButton();
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
            query = query.Where(track => track.RatingId is int ratingId && selectedRatingIds.Contains(ratingId));

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

    private void OnReviewFilterClicked(object? sender, RoutedEventArgs e)
    {
        _showReviewOnly = !_showReviewOnly;
        ApplyFilter();
    }

    private void UpdateReviewFilterButton()
    {
        var count = _allItems.Count(item => item.NeedsReview);
        ReviewFilterBtn.Opacity = _showReviewOnly ? 1.0 : 0.35;
        ToolTip.SetTip(ReviewFilterBtn, _showReviewOnly
            ? $"Review filter: On ({count})"
            : $"Reviews ({count})");
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
        _showReviewOnly = false;
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

    private void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        ImportOverlay.Open();
    }

    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        UpdateSettingsLayout();
        SettingsOverlay.Open();
    }

    private void UpdateSettingsLayout()
    {
        SettingsOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
    }

    private async void ShowToast(string message)
    {
        _toastCts?.Cancel();
        var cts = new CancellationTokenSource();
        _toastCts = cts;

        ToastText.Text = message;
        Toast.IsVisible = true;

        try
        {
            await Task.Delay(3500, cts.Token);
            if (!cts.IsCancellationRequested)
                Toast.IsVisible = false;
        }
        catch (OperationCanceledException) { }
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

    private void OnContextToggleReviewClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count)
            return;

        ToggleReview(_filteredItems[idx].Track);
    }

    private async void OnContextDeleteClicked(object? sender, RoutedEventArgs e)
    {
        var idx = FileList.SelectedIndex;
        if (idx < 0 || idx >= _filteredItems.Count)
            return;

        var track = _filteredItems[idx].Track;
        var error = await MusicLibraryService.Current.DeleteTrackAsync(track);
        if (error is not null)
        {
            ShowToast(error);
            return;
        }

        RefreshTrackList();
        ShowToast("Track deleted");
    }

    // ─── Playback control ─────────────────────────────────────────────────────

    private void OnListDoubleTapped(object? sender, TappedEventArgs e) => StartPlayback();
    private void OnPreviousClicked(object? sender, RoutedEventArgs e) => NavigatePrevious();
    private void OnNextClicked(object? sender, RoutedEventArgs e) => NavigateNext(isManual: true);

    private void OnPlayPauseClicked(object? sender, RoutedEventArgs e) => TogglePlayPause();

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (IsTextEntry(e.Source)) return;
        var shortcut = e.Key switch
        {
            Key.Space or Key.MediaPlayPause => MediaShortcut.PlayPause,
            Key.MediaPreviousTrack => MediaShortcut.Previous,
            Key.MediaNextTrack => MediaShortcut.Next,
            _ => (MediaShortcut?)null
        };
        if (shortcut is null) return;
        HandleMediaShortcut(shortcut.Value);
        e.Handled = true;
    }

    private void OnGlobalMediaKeyPressed(MediaShortcut shortcut) => HandleMediaShortcut(shortcut);

    private void HandleMediaShortcut(MediaShortcut shortcut)
    {
        switch (shortcut)
        {
            case MediaShortcut.Previous: NavigatePrevious(); break;
            case MediaShortcut.PlayPause: TogglePlayPause(); break;
            case MediaShortcut.Next: NavigateNext(isManual: true); break;
        }
    }

    private static bool IsTextEntry(object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
            if (visual is TextBox or ComboBox)
                return true;
        return false;
    }

    private void TogglePlayPause()
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

    private void OnReviewToggleClicked(object? sender, RoutedEventArgs e)
    {
        var track = ActiveOrSelectedTrack();
        if (track is null)
        {
            ShowToast("No track selected");
            return;
        }

        ToggleReview(track);
    }

    private MusicTrack? ActiveOrSelectedTrack()
    {
        if (_engine.ActiveTrackId >= 0)
            return _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track;

        var index = FileList.SelectedIndex;
        return index >= 0 && index < _filteredItems.Count
            ? _filteredItems[index].Track
            : null;
    }

    private void ToggleReview(MusicTrack track)
    {
        var needsReview = !track.NeedsReview;
        MusicLibraryService.Current.SetTrackNeedsReview(track.Id, needsReview);
        RefreshTrackList();
        ShowToast(needsReview ? "Marked for review" : "Review mark removed");
    }

    private void UpdateReviewButton()
    {
        var track = ActiveOrSelectedTrack();
        var isMarked = track?.NeedsReview == true;
        ReviewBtn.Opacity = isMarked ? 1.0 : 0.35;
        ToolTip.SetTip(ReviewBtn, isMarked ? "Remove review mark" : "Mark for review");
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

        if (_engine.ActiveTrackId >= 0 && _engine.ActiveTrackId != track.Id)
        {
            var totalSeconds = _engine.TotalTime.TotalSeconds;
            var playedFraction = totalSeconds > 0 ? _engine.CurrentTime.TotalSeconds / totalSeconds : 1;
            FinishListeningSession(markSkipped: !isCrossfade && playedFraction < .8);
        }

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

        BeginListeningSession(track.Id);

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
        _windowsMediaSession.UpdateState(_engine.State);
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
        FinishListeningSession(markSkipped: false);
        NavigateNext(isManual: false);
    }

    private void OnProgressUpdated()
    {
        RecordListeningProgress();
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
        UpdatePlaylistSummary();
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
        UpdatePlaylistSummary();
    }

    private void UpdatePlaylistSummary()
    {
        var totalSeconds = _filteredItems
            .Select(item => item.Track.DurationSeconds ?? 0)
            .Sum();

        var summary = $"{_filteredItems.Count} tracks · {FormatPlaylistDuration(totalSeconds)}";
        var remaining = RemainingPlaylistDurationSeconds();
        if (remaining > 0)
            summary += $" · ends {DateTime.Now.AddSeconds(remaining):HH:mm}";

        PlaylistSummaryText.Text = summary;
    }

    private int RemainingPlaylistDurationSeconds()
    {
        var currentIdx = GetCurrentPlayIndex();
        if (currentIdx < 0 || currentIdx >= _filteredItems.Count)
            return 0;

        var currentRemaining = _engine.TotalTime.TotalSeconds > 0
            ? Math.Max(0, (int)(_engine.TotalTime - _engine.CurrentTime).TotalSeconds)
            : _filteredItems[currentIdx].Track.DurationSeconds ?? 0;

        return currentRemaining + _filteredItems
            .Skip(currentIdx + 1)
            .Select(item => item.Track.DurationSeconds ?? 0)
            .Sum();
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
        var totalSeconds = _engine.TotalTime.TotalSeconds;
        var playedFraction = totalSeconds > 0 ? _engine.CurrentTime.TotalSeconds / totalSeconds : 1;
        FinishListeningSession(markSkipped: playedFraction < .8);
        _engine.Stop();
    }

    // ─── Listening telemetry ─────────────────────────────────────────────────

    private void BeginListeningSession(int trackId)
    {
        _listeningTrackId = trackId;
        _lastListeningPositionSeconds = 0;
        _unflushedListeningSeconds = 0;
        MusicLibraryService.Current.RecordTrackPlaybackStarted(trackId);
        EditTrackOverlay.RefreshUsageStats();
    }

    private void RecordListeningProgress()
    {
        if (_listeningTrackId < 0 || _listeningTrackId != _engine.ActiveTrackId)
            return;

        var position = _engine.CurrentTime.TotalSeconds;
        var delta = position - _lastListeningPositionSeconds;
        // Seeking is not listening time; accept only the small deltas emitted by the playback timer.
        if (delta is > 0 and <= 1.5)
            _unflushedListeningSeconds += delta;
        _lastListeningPositionSeconds = position;
        FlushListeningSeconds();
    }

    private void FinishListeningSession(bool markSkipped)
    {
        if (_listeningTrackId < 0) return;
        RecordListeningProgress();
        FlushListeningSeconds(force: true);
        if (markSkipped)
            MusicLibraryService.Current.RecordTrackSkip(_listeningTrackId);
        EditTrackOverlay.RefreshUsageStats();
        _listeningTrackId = -1;
        _lastListeningPositionSeconds = 0;
        _unflushedListeningSeconds = 0;
    }

    private void FlushListeningSeconds(bool force = false)
    {
        var wholeSeconds = (int)Math.Floor(_unflushedListeningSeconds);
        if (wholeSeconds < (force ? 1 : 5)) return;
        MusicLibraryService.Current.AddTrackListenedSeconds(_listeningTrackId, wholeSeconds);
        _unflushedListeningSeconds -= wholeSeconds;
        EditTrackOverlay.RefreshUsageStats();
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
        UpdateReviewButton();
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

    private static string FormatPlaylistDuration(int totalSeconds)
    {
        var time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}h"
            : $"{time.Minutes}:{time.Seconds:D2}m";
    }

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
