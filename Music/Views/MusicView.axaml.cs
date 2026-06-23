using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Music.Core;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class MusicView : UserControl
{
    private const string DefaultFilterPresetName = "Default";

    // Engine
    private readonly PlaybackEngine _engine = new();
    private readonly GlobalMediaKeyListener _globalMediaKeys = new();
    private readonly WindowsMediaSession _windowsMediaSession = new();
    private readonly DispatcherTimer _atmosphereTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly SolidColorBrush _appAtmosphereBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _playerAtmosphereBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _playerTopGlowBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _playerChromeEdgeBrush = new(Color.Parse("#30283A46"));
    private bool _isSeeking;
    private double _targetEnergy;
    private double _targetBass;
    private double _targetTreble;
    private double _visualEnergy;
    private double _visualBass;
    private double _visualTreble;

    // Playback settings
    private bool _shuffle;

    // UI state
    private bool _filterPanelVisible;
    private CancellationTokenSource? _thumbLoadCts;
    private CancellationTokenSource? _toastCts;
    private readonly Dictionary<int, Bitmap?> _playerArtworkCache = [];

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private int _nextTrackIndex = -1;
    private int _listeningTrackId = -1;
    private double _lastListeningPositionSeconds;
    private double _unflushedListeningSeconds;
    private PlaybackEngineSnapshot? _previewPlaybackSnapshot;
    private bool _isTrackPreviewActive;
    private int _previewTrackId = -1;

    private readonly Random _rng = new();

    // Track list data
    private List<TrackDisplayItem> _allItems = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private Dictionary<int, List<int>> _allTrackTagIds = [];
    private List<TrackDisplayItem> _filteredItems = [];
    private List<PortableFilterPreset> _filterPresets = [];
    private bool _updatingPresetUi;
    private bool _showReviewOnly;
    private MultiSelectFilterControl? _conditionGenreCtrl;
    private MultiSelectFilterControl? _conditionTagCtrl;
    private FilterSection? _conditionGenreSection;
    private FilterSection? _conditionTagSection;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreCtrl,
        MultiSelectFilterControl TagCtrl,
        Action RefreshVisuals);
    private sealed record FilterSection(Control Control, Action Refresh);
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
            _atmosphereTimer.Stop();
            _engine.Dispose();
            _globalMediaKeys.Dispose();
            _windowsMediaSession.Dispose();
            ClearPlayerArtworkBackground(disposeCache: true);
        };

        // Engine events
        _engine.StateChanged += OnEngineStateChanged;
        _engine.TrackNaturallyEnded += OnTrackNaturallyEnded;
        _engine.ProgressUpdated += OnProgressUpdated;
        _engine.AudioLevelUpdated += OnAudioLevelUpdated;
        _atmosphereTimer.Tick += (_, _) => UpdateAudioReactiveAtmosphere();
        AppAtmosphereTint.Fill = _appAtmosphereBrush;
        PlayerAtmosphereTint.Background = _playerAtmosphereBrush;
        PlayerChromeEdge.Background = _playerChromeEdgeBrush;
        PlayerTopGlow.Background = _playerTopGlowBrush;

        // Seeking
        PlaybackSlider.AddHandler(PointerPressedEvent,
            OnSliderPointerPressed, RoutingStrategies.Tunnel);
        PlaybackSlider.AddHandler(PointerReleasedEvent,
            OnSliderPointerReleased, RoutingStrategies.Tunnel);

        SearchBox.TextChanged += (_, _) =>
        {
            ApplyFilter();
            UpdateSearchVisibility();
        };
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        FileList.SelectionChanged += (_, _) =>
        {
            UpdateReviewButton();
        };
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
        BackgroundAnalysisService.Current.Initialize();
        UpdateQueueStatus();
        UpdateImportBounds();

        LoadLookups();
        LoadFilterPresets();
        InitializeFilterConditionBuilder();
        if (_filterPresets.FirstOrDefault(preset =>
                string.Equals(preset.Name, DefaultFilterPresetName, StringComparison.OrdinalIgnoreCase)) is { } defaultPreset)
            ApplyFilterPreset(defaultPreset);
        else
            RebuildFilterConditionsPanel();
        RefreshTrackList();

        SettingsOverlay.PreloadGenreVocabulary();

        AddTrackOverlay.TrackDownloaded += warning =>
        {
            AddTrackOverlay.IsVisible = false;
            RefreshTrackList();
            ShowToast(warning ?? "Track downloaded; analysis queued");
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
        BackgroundAnalysisService.Current.QueueChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RefreshTrackList();
            if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
        });
        BackgroundAnalysisService.Current.TrackAnalysisFinished += (_, _) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                RefreshTrackList();
                if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
            });
        EditTrackOverlay.TrackSaved += RefreshTrackList;
        EditTrackOverlay.PreviewRequested += StartTrackPreview;
        EditTrackOverlay.PreviewClosed += StopTrackPreview;
        SettingsOverlay.ToastRequested += ShowToast;
        SettingsOverlay.LibraryMetadataChanged += RefreshLibraryPresentation;
        SettingsOverlay.ExportRequested += ExportPortableLibrary;
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
        Dispatcher.UIThread.Post(() =>
        {
            if (_filteredItems.Count == 0 || _engine.State != EngineState.Stopped)
                return;

            FileList.SelectedIndex = 0;
            StartPlayback();
            _engine.Pause();
            UpdatePlaybackPositionUi();
            UpdateButtonStates();
        }, DispatcherPriority.Background);
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
        Values.Tags = MusicLibraryService.Current.GetTags();
        Values.Styles = MusicLibraryService.Current.GetStyles();
        Values.Ratings = MusicLibraryService.Current.GetRatings();

        RatingFilter.SetItems(Values.Ratings.Select(r => r.Name));

        if (_conditionGenreCtrl is not null && _conditionTagCtrl is not null)
        {
            _conditionGenreCtrl.SetItems(GenreFilterOptions());
            _conditionTagCtrl.SetItems(TagFilterOptions());
            RefreshConditionBuilder();
        }

        foreach (var fg in _filterGroups)
        {
            fg.GenreCtrl.SetItems(GenreFilterOptions());
            fg.TagCtrl.SetItems(TagFilterOptions());
        }
        RebuildFilterConditionsPanel();
    }

    private void RefreshTrackList()
    {
        _thumbLoadCts?.Cancel();
        _thumbLoadCts = new CancellationTokenSource();

        ClearPlayerArtworkBackground(disposeCache: true);

        foreach (var item in _allItems)
            item.Thumbnail?.Dispose();

        var tracks = MusicLibraryService.Current.GetTracks();
        var unanalyzedTrackIds = MusicLibraryService.Current.GetUnanalyzedTracks()
            .Select(track => track.Id)
            .ToHashSet();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackTagIds = MusicLibraryService.Current.GetAllTrackTagIds();

        var genreMap = Values.Genres.ToDictionary(g => g.Id, g => g.Name);
        var tagMap = Values.Tags.ToDictionary(t => t.Id);
        var tagOrder = Values.Tags.Select((tag, index) => (tag.Id, index))
            .ToDictionary(item => item.Id, item => item.index);
        var ratingMap = Values.Ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = Values.Styles.ToDictionary(s => s.Id, s => s.Name);

        _allItems = tracks.Select(t =>
        {
            var genreIds = _allTrackGenreIds.GetValueOrDefault(t.Id, []);
            var tagIds = _allTrackTagIds.GetValueOrDefault(t.Id, []);
            var styleIds = _allTrackStyleIds.GetValueOrDefault(t.Id, []);

            var modelGenreAssignments = MusicLibraryService.Current.GetTrackModelGenres(t.Id)
                .Where(assignment => assignment.IsEnabled)
                .ToList();
            var modelGenreStr = string.Join(", ", modelGenreAssignments
                .Where(assignment => assignment.Reasons.Count > 0)
                .Select(assignment => ShortGenreName(assignment.GenreName))
                .Where(name => name.Length > 0)
                .Order());
            var manualGenreStr = string.Join(", ", modelGenreAssignments
                .Where(assignment => assignment.Reasons.Count == 0)
                .Select(assignment => ShortGenreName(assignment.GenreName))
                .Where(name => name.Length > 0)
                .Order());
            var genreStr = string.Join(", ", new[] { modelGenreStr, manualGenreStr }
                .Where(text => !string.IsNullOrWhiteSpace(text)));
            if (string.IsNullOrWhiteSpace(genreStr))
            {
                genreStr = string.Join(", ", genreIds
                    .Select(id => genreMap.GetValueOrDefault(id, ""))
                    .Select(ShortGenreName)
                    .Where(n => n.Length > 0).Order());
            }
            var styleStr = string.Join(", ", styleIds
                .Select(id => styleMap.GetValueOrDefault(id, ""))
                .Where(n => n.Length > 0).Order());
            var trackTags = tagIds
                .Select(id => tagMap.GetValueOrDefault(id))
                .Where(tag => tag is not null)
                .Cast<Tag>()
                .OrderBy(tag => tagOrder.GetValueOrDefault(tag.Id, int.MaxValue))
                .ToList();
            var tagDisplays = trackTags
                .Select(tag => new TrackTagDisplay(
                    tag.Name,
                    tag.CategoryName,
                    CategoryBrush(tag.CategoryColor)))
                .ToList();
            var ratingName = t.RatingId is int ratingId ? ratingMap.GetValueOrDefault(ratingId, "") : "Not rated";
            var durationText = t.DurationSeconds.HasValue ? FormatDuration(t.DurationSeconds.Value) : "";
            var attributes = MusicLibraryService.Current.GetTrackDerivedAttributes(t.Id);
            var profileText = string.Join(" · ", attributes
                .Where(attribute => attribute.Key is "emotional_tone" or "energy_context" or "intensity")
                .Select(attribute => $"{attribute.EffectiveValue} {ProfileAttributeName(attribute.Key)}"));
            
            return new TrackDisplayItem(t, genreStr, modelGenreStr, manualGenreStr, styleStr, durationText, ratingName, profileText, tagDisplays, t.ChannelName ?? "")
            {
                NeedsReview = t.NeedsReview,
                NeedsAnalysis = unanalyzedTrackIds.Contains(t.Id)
            };
        }).ToList();

        ApplyFilter();
        if (_engine.ActiveTrackId >= 0
            && _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track is { } activeTrack)
            UpdatePlayerArtworkBackground(activeTrack);
        _ = LoadThumbnailsAsync(_thumbLoadCts.Token);
    }

    private static string ProfileAttributeName(string key) => key switch
    {
        "emotional_tone" => "Tone",
        "energy_context" => "Energy",
        _ => char.ToUpperInvariant(key[0]) + key[1..]
    };

    private static string ShortGenreName(string genreName)
    {
        var separator = genreName.LastIndexOf('→');
        return separator >= 0 && separator + 1 < genreName.Length
            ? genreName[(separator + 1)..].Trim()
            : genreName;
    }

    private static string TagFilterName(Tag tag) => $"{tag.CategoryName}: {tag.Name}";

    private static IEnumerable<MultiSelectFilterControl.FilterOption> GenreFilterOptions() =>
        Values.Genres.Select(genre =>
        {
            var parts = genre.Name.Split('→', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2
                ? new MultiSelectFilterControl.FilterOption(genre.Name, parts[1], parts[0])
                : new MultiSelectFilterControl.FilterOption(genre.Name, genre.Name);
        });

    private static IEnumerable<MultiSelectFilterControl.FilterOption> TagFilterOptions() =>
        Values.Tags.Select(tag => new MultiSelectFilterControl.FilterOption(
            TagFilterName(tag),
            tag.Name,
            tag.CategoryName,
            tag.CategoryColor));

    private static IBrush CategoryBrush(string? color)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? "#65BCEB" : color)); }
        catch { return new SolidColorBrush(Color.Parse("#65BCEB")); }
    }

    private static IBrush SafeBrush(string? color, string fallback)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? fallback : color)); }
        catch { return new SolidColorBrush(Color.Parse(fallback)); }
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
                new HashSet<int>(),
                SelectedIds(fg.TagCtrl.SelectedItems, Values.Tags, TagFilterName, t => t.Id)))
            .ToList();
        
        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackGenreIds,
            _allTrackStyleIds,
            _allTrackTagIds,
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

        var selectedTrackId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id;

        FileList.ItemsSource = _filteredItems;
        RestoreOrInitializeSelection(selectedTrackId);
        UpdatePlaylistSummary();
        RefreshNextTrackPreview();
        UpdateFilterCounts();
        UpdateReviewFilterButton();
        UpdateReviewButton();
    }

    private void RestoreOrInitializeSelection(int? previousSelectedTrackId)
    {
        if (_filteredItems.Count == 0)
        {
            FileList.SelectedIndex = -1;
            return;
        }

        var targetTrackId = _engine.ActiveTrackId >= 0
            ? _engine.ActiveTrackId
            : previousSelectedTrackId;

        if (targetTrackId is int id)
        {
            var index = _filteredItems.FindIndex(item => item.Track.Id == id);
            if (index >= 0)
            {
                FileList.SelectedIndex = index;
                return;
            }
        }

        if (FileList.SelectedIndex < 0 || FileList.SelectedIndex >= _filteredItems.Count)
            FileList.SelectedIndex = 0;

        FileList.ScrollIntoView(_filteredItems[FileList.SelectedIndex]);
    }

    private void UpdateFilterCounts()
    {
        foreach (var fg in _filterGroups)
        {
            var groupTracks = TracksMatchingSearchRatingAndGroup(fg);
            var groupTrackIds = groupTracks.Select(track => track.Id).ToList();

            var genreFacetCounts = MetadataCountService.FacetCounts(groupTrackIds, _allTrackGenreIds);
            var tagFacetCounts = MetadataCountService.FacetCounts(groupTrackIds, _allTrackTagIds);

            var genreCountByName = Values.Genres.ToDictionary(g => g.Name,
                g => genreFacetCounts.GetValueOrDefault(g.Id, 0));
            var tagCountByName = Values.Tags.ToDictionary(TagFilterName,
                t => tagFacetCounts.GetValueOrDefault(t.Id, 0));

            fg.GenreCtrl.UpdateCounts(genreCountByName);
            fg.TagCtrl.UpdateCounts(tagCountByName);
        }
    }

    private List<MusicTrack> TracksMatchingSearchRatingAndGroup(FilterGroupControls group)
    {
        IEnumerable<MusicTrack> query = _allItems.Select(item => item.Track);
        var selectedRatingIds = SelectedIds(RatingFilter.SelectedItems, Values.Ratings, r => r.Name, r => r.Id);
        var selectedGenreIds = SelectedIds(group.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id);
        var selectedTagIds = SelectedIds(group.TagCtrl.SelectedItems, Values.Tags, TagFilterName, t => t.Id);
        var term = SearchBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(track => track.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (selectedRatingIds.Count > 0)
            query = query.Where(track => track.RatingId is int ratingId && selectedRatingIds.Contains(ratingId));

        if (selectedGenreIds.Count > 0)
            query = query.Where(track => TrackHasAllTags(track.Id, _allTrackGenreIds, selectedGenreIds));

        if (selectedTagIds.Count > 0)
            query = query.Where(track => TrackHasAllTags(track.Id, _allTrackTagIds, selectedTagIds));

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
        FiltersToggleBtn.Opacity = _filterPanelVisible ? 1.0 : 0.86;
    }

    private void OnSearchToggleClicked(object? sender, RoutedEventArgs e)
    {
        if (SearchBox.IsVisible)
        {
            SearchBox.Text = string.Empty;
            SearchBox.IsVisible = false;
            SearchToggleBtn.Opacity = 0.86;
            SearchToggleBtn.Focus();
            return;
        }

        SearchBox.IsVisible = true;
        SearchToggleBtn.Opacity = 1.0;
        Dispatcher.UIThread.Post(() => SearchBox.Focus(), DispatcherPriority.Background);
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchBox.Text = string.Empty;
            SearchBox.IsVisible = false;
            SearchToggleBtn.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            SearchToggleBtn.Focus();
            e.Handled = true;
        }
    }

    private void OnSearchBoxLostFocus(object? sender, RoutedEventArgs e) => UpdateSearchVisibility();

    private void UpdateSearchVisibility()
    {
        var hasSearch = !string.IsNullOrWhiteSpace(SearchBox.Text);
        if (!SearchBox.IsKeyboardFocusWithin && !hasSearch)
            SearchBox.IsVisible = false;
        SearchToggleBtn.Opacity = SearchBox.IsVisible || hasSearch ? 1.0 : 0.86;
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
        PresetBox.SelectedItem = DefaultFilterPresetName;
        PresetNameBox.Text = DefaultFilterPresetName;
        _updatingPresetUi = false;

        RatingFilter.SetItems(Values.Ratings.Select(r => r.Name));
        RatingFilter.Placeholder = "All ratings";
        _filterGroups.Clear();
        RebuildFilterConditionsPanel();
        ClearConditionBuilder();
        _showReviewOnly = false;
        ApplyFilter();
    }

    private void LoadFilterPresets()
    {
        _filterPresets = FilterPresetStore.Load();
        if (_filterPresets.All(preset => !string.Equals(preset.Name, DefaultFilterPresetName, StringComparison.OrdinalIgnoreCase)))
        {
            _filterPresets.Add(new PortableFilterPreset(
                DefaultFilterPresetName,
                new List<string>(),
                new List<PortableFilterGroup>()));
            FilterPresetStore.Save(_filterPresets);
            _filterPresets = FilterPresetStore.Load();
        }

        PresetNameBox.Text = DefaultFilterPresetName;
        RefreshPresetBox(DefaultFilterPresetName);
    }

    private void RefreshPresetBox(string? selectedName = null)
    {
        _updatingPresetUi = true;

        var names = _filterPresets
            .Select(preset => preset.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        PresetBox.ItemsSource = names;
        var effectiveSelection = selectedName ?? DefaultFilterPresetName;
        PresetBox.SelectedItem = names.Contains(effectiveSelection, StringComparer.OrdinalIgnoreCase)
            ? names.First(name => string.Equals(name, effectiveSelection, StringComparison.OrdinalIgnoreCase))
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

        if (string.Equals(name, DefaultFilterPresetName, StringComparison.OrdinalIgnoreCase))
        {
            var defaultPreset = new PortableFilterPreset(
                DefaultFilterPresetName,
                new List<string>(),
                new List<PortableFilterGroup>());
            var defaultIndex = _filterPresets.FindIndex(preset =>
                string.Equals(preset.Name, DefaultFilterPresetName, StringComparison.OrdinalIgnoreCase));
            if (defaultIndex >= 0) _filterPresets[defaultIndex] = defaultPreset;
            else _filterPresets.Add(defaultPreset);
        }
        else
        {
            _filterPresets.RemoveAll(preset =>
                string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();
        PresetNameBox.Text = DefaultFilterPresetName;
        RefreshPresetBox(DefaultFilterPresetName);
        ApplyFilterPreset(_filterPresets.First(preset =>
            string.Equals(preset.Name, DefaultFilterPresetName, StringComparison.OrdinalIgnoreCase)));
    }

    private PortableFilterPreset CreatePreset(string name)
    {
        var groups = _filterGroups
            .Select(group => new PortableFilterGroup(
                SortedNames(group.GenreCtrl.SelectedItems),
                new List<string>(),
                SortedNames(group.TagCtrl.SelectedItems)))
            .Where(group => group.Genres.Count > 0 || (group.Tags?.Count ?? 0) > 0)
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

        var groups = preset.Groups
            .Where(group => group.Genres.Count > 0 || (group.Tags?.Count ?? 0) > 0)
            .ToList();

        foreach (var group in groups)
            _filterGroups.Add(CreateFilterCondition(group.Genres, group.Tags ?? new List<string>()));

        RebuildFilterConditionsPanel();
        ClearConditionBuilder();
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
        AddConditionFromBuilder();
        ApplyFilter();
    }

    private void InitializeFilterConditionBuilder()
    {
        _conditionGenreCtrl = new MultiSelectFilterControl { Placeholder = "All genres" };
        _conditionGenreCtrl.SetItems(GenreFilterOptions());

        _conditionTagCtrl = new MultiSelectFilterControl { Placeholder = "All tags" };
        _conditionTagCtrl.SetItems(TagFilterOptions());

        _conditionGenreSection = CreateGenreFilterSection(_conditionGenreCtrl);
        _conditionTagSection = CreateTagFilterSection(_conditionTagCtrl);

        FilterBuilderPanel.Children.Clear();
        FilterBuilderPanel.Children.Add(_conditionGenreSection.Control);
        FilterBuilderPanel.Children.Add(_conditionTagSection.Control);
    }

    private void AddConditionFromBuilder()
    {
        if (_conditionGenreCtrl is null || _conditionTagCtrl is null)
            return;

        var selectedGenres = SortedNames(_conditionGenreCtrl.SelectedItems);
        var selectedTags = SortedNames(_conditionTagCtrl.SelectedItems);
        if (selectedGenres.Count == 0 && selectedTags.Count == 0)
            return;

        _filterGroups.Add(CreateFilterCondition(selectedGenres, selectedTags));
        RebuildFilterConditionsPanel();
        ClearConditionBuilder();
    }

    private FilterGroupControls CreateFilterCondition(IEnumerable<string> genres, IEnumerable<string> tags)
    {
        var genreCtrl = new MultiSelectFilterControl { Placeholder = "All genres" };
        genreCtrl.SetItems(GenreFilterOptions());
        genreCtrl.SetSelectedItems(genres, notify: false);

        var tagCtrl = new MultiSelectFilterControl { Placeholder = "All tags" };
        tagCtrl.SetItems(TagFilterOptions());
        tagCtrl.SetSelectedItems(tags, notify: false);

        return new FilterGroupControls(genreCtrl, tagCtrl, () => { });
    }

    private void ClearConditionBuilder()
    {
        _conditionGenreCtrl?.SetSelectedItems(Array.Empty<string>(), notify: false);
        _conditionTagCtrl?.SetSelectedItems(Array.Empty<string>(), notify: false);
        RefreshConditionBuilder();
    }

    private void RefreshConditionBuilder()
    {
        _conditionGenreSection?.Refresh();
        _conditionTagSection?.Refresh();
    }

    private void RebuildFilterConditionsPanel()
    {
        FilterGroupsPanel.Children.Clear();

        if (_filterGroups.Count == 0)
        {
            FilterGroupsPanel.Children.Add(new TextBlock
            {
                Text = "No conditions yet. Select genres or tags above, then add a condition.",
                FontSize = 11,
                Opacity = 0.52,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0)
            });
            return;
        }

        for (var i = 0; i < _filterGroups.Count; i++)
            FilterGroupsPanel.Children.Add(CreateConditionCard(_filterGroups[i], i));
    }

    private Control CreateConditionCard(FilterGroupControls condition, int index)
    {
        var genreNames = condition.GenreCtrl.SelectedItems
            .Select(DisplayGenreFilterName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var selectedTags = condition.TagCtrl.SelectedItems
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var chips = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var name in genreNames)
            chips.Children.Add(CreateConditionChip(name, "#65BCEB"));
        foreach (var selectedTag in selectedTags)
        {
            var tag = Values.Tags.FirstOrDefault(item => string.Equals(TagFilterName(item), selectedTag, StringComparison.OrdinalIgnoreCase));
            chips.Children.Add(CreateConditionChip(DisplayTagFilterName(selectedTag), tag?.CategoryColor ?? "#CFA7FF"));
        }

        var removeBtn = new Button
        {
            Content = "Remove",
            Padding = new Thickness(9, 4),
            FontSize = 10.5,
            Opacity = 0.68,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = new SolidColorBrush(Color.Parse("#314B5F")),
            BorderThickness = new Thickness(1)
        };
        removeBtn.Click += (_, _) => RemoveFilterGroup(condition);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        header.Children.Add(new TextBlock
        {
            Text = $"Condition {index + 1}",
            FontSize = 11.5,
            FontWeight = FontWeight.SemiBold,
            Opacity = 0.82,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(removeBtn, 1);
        header.Children.Add(removeBtn);

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#78111820")),
            BorderBrush = new SolidColorBrush(Color.Parse("#263442")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 9),
            Child = new StackPanel
            {
                Spacing = 8,
                Children = { header, chips }
            }
        };
    }

    private static Border CreateConditionChip(string text, string? accentColor)
    {
        var accent = SafeBrush(accentColor, "#65BCEB");
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1A2026")),
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 3),
            Margin = new Thickness(0, 0, 6, 6),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = accent
            }
        };
    }

    private static string DisplayGenreFilterName(string genreName)
    {
        var parts = genreName.Split('→', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : genreName;
    }

    private static string DisplayTagFilterName(string tagName)
    {
        var parts = tagName.Split(':', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : tagName;
    }

    private FilterSection CreateGenreFilterSection(MultiSelectFilterControl genreCtrl)
    {
        string? selectedGroupName = null;
        var searchText = string.Empty;
        var choicesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        var summary = new TextBlock
        {
            FontSize = 10.5,
            Opacity = 0.56,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var categoryBox = new ComboBox
        {
            Height = 34,
            Background = new SolidColorBrush(Color.Parse("#0D151D")),
            BorderBrush = new SolidColorBrush(Color.Parse("#414B5F")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(9, 4)
        };
        var searchBox = new TextBox
        {
            Watermark = "Search subgenre…",
            MinWidth = 180,
            Height = 34,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        string GroupName(Genre genre)
        {
            var parts = genre.Name.Split('→', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? parts[0] : string.Empty;
        }

        string SubgenreName(Genre genre)
        {
            var parts = genre.Name.Split('→', 2, StringSplitOptions.TrimEntries);
            return parts.Length == 2 ? parts[1] : genre.Name;
        }

        void FillChoices()
        {
            choicesPanel.Children.Clear();
            var selected = genreCtrl.SelectedItems;
            var normalizedSearch = searchText.Trim();
            var choices = Values.Genres
                .Where(genre => selectedGroupName is null || string.Equals(GroupName(genre), selectedGroupName, StringComparison.OrdinalIgnoreCase))
                .Where(genre => string.IsNullOrWhiteSpace(normalizedSearch)
                                || SubgenreName(genre).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)
                                || GroupName(genre).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(genre => selected.Contains(genre.Name))
                .ThenBy(genre => GroupName(genre), StringComparer.OrdinalIgnoreCase)
                .ThenBy(genre => SubgenreName(genre), StringComparer.OrdinalIgnoreCase)
                .Take(72)
                .ToList();

            foreach (var genre in choices)
            {
                var isSelected = selected.Contains(genre.Name);
                var button = CreateFilterChoiceButton(SubgenreName(genre), GroupName(genre), isSelected, null);
                button.Click += (_, _) =>
                {
                    var next = genreCtrl.SelectedItems.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (!next.Add(genre.Name))
                        next.Remove(genre.Name);
                    genreCtrl.SetSelectedItems(next);
                    FillChoices();
                    UpdateGenreSummary();
                };
                choicesPanel.Children.Add(button);
            }

            if (choices.Count == 0)
            {
                choicesPanel.Children.Add(new TextBlock
                {
                    Text = "No genres match this filter.",
                    FontSize = 11,
                    Opacity = 0.52,
                    Margin = new Thickness(0, 8, 0, 4)
                });
            }
        }

        void UpdateGenreSummary()
        {
            var selectedCount = genreCtrl.SelectedItems.Count;
            summary.Text = selectedCount == 0
                ? $"{Values.Genres.Count} selectable"
                : $"{selectedCount} selected · {Values.Genres.Count - selectedCount} available";
        }

        var groupChoices = new[] { new GenreGroupChoice(null, "All model groups") }
            .Concat(Values.Genres
                .Select(GroupName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Select(name => new GenreGroupChoice(name, name)))
            .ToList();
        categoryBox.ItemsSource = groupChoices;
        categoryBox.SelectedIndex = 0;
        categoryBox.SelectionChanged += (_, _) =>
        {
            selectedGroupName = (categoryBox.SelectedItem as GenreGroupChoice)?.GroupName;
            FillChoices();
        };
        searchBox.TextChanged += (_, _) =>
        {
            searchText = searchBox.Text ?? string.Empty;
            FillChoices();
        };

        UpdateGenreSummary();
        FillChoices();

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,220,*,Auto"), ColumnSpacing = 10 };
        header.Children.Add(new TextBlock
        {
            Text = "Genres",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(categoryBox, 1);
        header.Children.Add(categoryBox);
        Grid.SetColumn(searchBox, 2);
        header.Children.Add(searchBox);
        Grid.SetColumn(summary, 3);
        header.Children.Add(summary);

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#68121A22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3A46")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 10),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    header,
                    new ScrollViewer
                    {
                        MaxHeight = 205,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        Content = choicesPanel
                    }
                }
            }
        };
        return new FilterSection(border, () =>
        {
            UpdateGenreSummary();
            FillChoices();
        });
    }

    private FilterSection CreateTagFilterSection(MultiSelectFilterControl tagCtrl)
    {
        var panel = new StackPanel { Spacing = 9 };

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        header.Children.Add(new TextBlock
        {
            Text = "Tags",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        var summary = new TextBlock
        {
            FontSize = 10.5,
            Opacity = 0.56,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(summary, 2);
        header.Children.Add(summary);
        panel.Children.Add(header);

        void UpdateSummary()
        {
            summary.Text = tagCtrl.SelectedItems.Count == 0
                ? "No tags selected"
                : $"{tagCtrl.SelectedItems.Count} selected";
        }

        void Toggle(Tag tag)
        {
            var value = TagFilterName(tag);
            var next = tagCtrl.SelectedItems.ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!next.Add(value))
                next.Remove(value);
            tagCtrl.SetSelectedItems(next);
            RebuildTags();
        }

        void RebuildTags()
        {
            while (panel.Children.Count > 1)
                panel.Children.RemoveAt(1);

            foreach (var category in Values.Tags.GroupBy(tag => new { tag.CategoryId, tag.CategoryName, tag.CategoryColor }))
            {
                var accent = SafeBrush(category.Key.CategoryColor, "#65BCEB");
                var section = new StackPanel { Spacing = 5 };
                var categoryHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("3,Auto,*"), ColumnSpacing = 7 };
                categoryHeader.Children.Add(new Border
                {
                    Background = accent,
                    CornerRadius = new CornerRadius(2),
                    Height = 13,
                    VerticalAlignment = VerticalAlignment.Center
                });
                var title = new TextBlock
                {
                    Text = category.Key.CategoryName,
                    FontSize = 10.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = accent,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(title, 1);
                categoryHeader.Children.Add(title);
                section.Children.Add(categoryHeader);

                var chips = new WrapPanel { Orientation = Orientation.Horizontal };
                foreach (var tag in category
                             .OrderByDescending(tag => tagCtrl.SelectedItems.Contains(TagFilterName(tag)))
                             .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase))
                {
                    var selected = tagCtrl.SelectedItems.Contains(TagFilterName(tag));
                    var button = CreateFilterChoiceButton(tag.Name, null, selected, category.Key.CategoryColor);
                    ToolTip.SetTip(button, string.IsNullOrWhiteSpace(tag.Description)
                        ? tag.CategoryName
                        : $"{tag.CategoryName}\n{tag.Description}");
                    button.Click += (_, _) => Toggle(tag);
                    chips.Children.Add(button);
                }

                section.Children.Add(chips);
                panel.Children.Add(section);
            }

            UpdateSummary();
        }

        RebuildTags();

        var border = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#68121A22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#2A3A46")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 10),
            Child = panel
        };
        return new FilterSection(border, RebuildTags);
    }

    private static Button CreateFilterChoiceButton(string title, string? subtitle, bool isSelected, string? accentColor)
    {
        var accent = SafeBrush(accentColor, "#65BCEB");
        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = isSelected ? accent : new SolidColorBrush(Color.Parse("#E8F1F7")),
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var content = new StackPanel
        {
            Spacing = 0,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { titleText }
        };
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            content.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 9,
                Opacity = 0.62,
                Foreground = new SolidColorBrush(Color.Parse("#A9BAC8")),
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
        }

        return new Button
        {
            Content = content,
            Width = 136,
            Height = string.IsNullOrWhiteSpace(subtitle) ? 29 : 42,
            Margin = new Thickness(0, 0, 7, 7),
            Padding = new Thickness(8, 2),
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.Parse(isSelected ? "#1A3140" : "#1A2026")),
            BorderBrush = isSelected ? accent : new SolidColorBrush(Color.Parse("#394653")),
            BorderThickness = new Thickness(1),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private sealed record GenreGroupChoice(string? GroupName, string Label)
    {
        public override string ToString() => Label;
    }

    private void RemoveFilterGroup(FilterGroupControls fg)
    {
        var idx = _filterGroups.IndexOf(fg);
        if (idx < 0) return;
        _filterGroups.RemoveAt(idx);
        RebuildFilterConditionsPanel();
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

    private async void ExportPortableLibrary()
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
            if (_isTrackPreviewActive && _previewTrackId >= 0)
            {
                var previewTrack = _allItems.FirstOrDefault(item => item.Track.Id == _previewTrackId)?.Track;
                if (previewTrack is not null) StartTrackPreview(previewTrack);
            }
            else
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
        UpdatePlayerArtworkBackground(track);
        PlaybackInfoPanel.IsVisible = true;
        _nextTrackIndex = PeekNextTrackIndex(filteredIndex);
        UpdateUpcomingBar();
        UpdateButtonStates();
        RefreshPlayingMarkers();
    }

    // ─── Temporary track-information preview ─────────────────────────────────

    private void StartTrackPreview(MusicTrack track)
    {
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
        if (!File.Exists(filePath))
        {
            ShowToast("The audio file for this track is not available.");
            return;
        }

        if (!_isTrackPreviewActive)
        {
            _previewPlaybackSnapshot = _engine.CaptureSnapshot();
            // A preview is neither a skip nor a separate full listen.
            FinishListeningSession(markSkipped: false);
            _isTrackPreviewActive = true;
        }

        try
        {
            _engine.Play(filePath, track.Id, 0, 0);
        }
        catch (Exception exception)
        {
            ShowToast($"Preview could not start: {exception.Message}");
            return;
        }

        _previewTrackId = track.Id;
        _isSeeking = false;
        var index = _filteredItems.FindIndex(item => item.Track.Id == track.Id);
        if (index >= 0) FileList.SelectedIndex = index;
        NowPlayingText.Text = $"Preview · {track.Title}";
        UpdatePlayerArtworkBackground(track);
        PlaybackInfoPanel.IsVisible = true;
        _nextTrackIndex = -1;
        _crossfadeTriggered = false;
        UpdateUpcomingBar();
        UpdateButtonStates();
        RefreshPlayingMarkers();
    }

    private void StopTrackPreview()
    {
        if (!_isTrackPreviewActive) return;

        var snapshot = _previewPlaybackSnapshot;
        _previewPlaybackSnapshot = null;
        _isTrackPreviewActive = false;
        _previewTrackId = -1;
        _isSeeking = false;

        try
        {
            _engine.RestoreSnapshot(snapshot);
        }
        catch (Exception exception)
        {
            _engine.Stop();
            ShowToast($"Previous playback could not be restored: {exception.Message}");
        }

        if (snapshot is null || _engine.ActiveTrackId < 0)
        {
            NowPlayingText.Text = string.Empty;
            ClearPlayerArtworkBackground();
            PlaybackInfoPanel.IsVisible = false;
            _nextTrackIndex = -1;
        }
        else if (_allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track is { } restoredTrack)
        {
            ResumeListeningSession(restoredTrack.Id, _engine.CurrentTime);
            var index = _filteredItems.FindIndex(item => item.Track.Id == restoredTrack.Id);
            if (index >= 0) FileList.SelectedIndex = index;
            NowPlayingText.Text = restoredTrack.Title;
            UpdatePlayerArtworkBackground(restoredTrack);
            PlaybackInfoPanel.IsVisible = true;
            _nextTrackIndex = index >= 0 ? PeekNextTrackIndex(index) : -1;
            _crossfadeTriggered = false;
        }

        UpdateUpcomingBar();
        UpdateButtonStates();
        RefreshPlayingMarkers();
    }

    // ─── Engine events ────────────────────────────────────────────────────────

    private void OnEngineStateChanged()
    {
        _windowsMediaSession.UpdateState(_engine.State);
        UpdateButtonStates();
        if (_engine.State == EngineState.Playing)
            StartAudioAtmosphereTimer();
        else
            FadeOutAudioAtmosphere();

        if (_engine.State == EngineState.Stopped)
        {
            _nextTrackIndex = -1;
            _crossfadeTriggered = false;
            _lastKnownActiveId = -1;
            if (!_isTrackPreviewActive)
                ClearPlayerArtworkBackground();
            RefreshPlayingMarkers();
            UpdateUpcomingBar();
        }
    }

    private void OnTrackNaturallyEnded()
    {
        if (_isTrackPreviewActive)
        {
            _nextTrackIndex = -1;
            UpdateUpcomingBar();
            return;
        }
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

        UpdatePlaybackPositionUi();

        if (!_isTrackPreviewActive && !_crossfadeTriggered && _nextTrackIndex >= 0 && _engine.State == EngineState.Playing)
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

    private void UpdatePlaybackPositionUi()
    {
        if (!_isSeeking)
        {
            PlaybackSlider.Value = _engine.TotalTime.TotalSeconds > 0
                ? _engine.CurrentTime.TotalSeconds / _engine.TotalTime.TotalSeconds * 100
                : 0;
        }

        PlaybackTimeText.Text =
            $"{FormatDuration(_engine.CurrentTime)} / {FormatDuration(_engine.TotalTime)}";
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
        if (_isTrackPreviewActive) return;
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
        if (_isTrackPreviewActive) return;
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

    private void UpdatePlayerArtworkBackground(MusicTrack track)
    {
        if (!_playerArtworkCache.TryGetValue(track.Id, out var artwork))
        {
            artwork = LoadPlayerArtwork(track);
            _playerArtworkCache[track.Id] = artwork;
        }

        PlayerArtworkBackground.Source = artwork;
        PlayerArtworkBackground.IsVisible = artwork is not null;
        AppArtworkBackground.Source = artwork;
        AppArtworkBackground.IsVisible = artwork is not null;
        ListArtworkBackground.Source = artwork;
        ListArtworkBackground.IsVisible = artwork is not null;
    }

    private static Bitmap? LoadPlayerArtwork(MusicTrack track)
    {
        try
        {
            var audioFilePath = Path.Combine(Values.TracksDirectory, track.FileName);
            var artwork = ThumbnailService.ReadEmbeddedArtwork(audioFilePath);
            if (artwork is null)
                return null;

            using var stream = new MemoryStream(artwork);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    private void ClearPlayerArtworkBackground(bool disposeCache = false)
    {
        PlayerArtworkBackground.Source = null;
        PlayerArtworkBackground.IsVisible = false;
        AppArtworkBackground.Source = null;
        AppArtworkBackground.IsVisible = false;
        ListArtworkBackground.Source = null;
        ListArtworkBackground.IsVisible = false;
        ResetAudioAtmosphere();

        if (!disposeCache)
            return;

        foreach (var artwork in _playerArtworkCache.Values)
            artwork?.Dispose();
        _playerArtworkCache.Clear();
    }

    // ─── Audio-reactive atmosphere ───────────────────────────────────────────

    private void OnAudioLevelUpdated(PlaybackAudioLevel level)
    {
        if (_engine.State != EngineState.Playing)
            return;

        _targetEnergy = level.Energy;
        _targetBass = level.Bass;
        _targetTreble = level.Treble;
        StartAudioAtmosphereTimer();
    }

    private void StartAudioAtmosphereTimer()
    {
        if (!_atmosphereTimer.IsEnabled)
            _atmosphereTimer.Start();
    }

    private void FadeOutAudioAtmosphere()
    {
        _targetEnergy = 0;
        _targetBass = 0;
        _targetTreble = 0;
        StartAudioAtmosphereTimer();
    }

    private void ResetAudioAtmosphere()
    {
        _targetEnergy = 0;
        _targetBass = 0;
        _targetTreble = 0;
        _visualEnergy = 0;
        _visualBass = 0;
        _visualTreble = 0;
        ApplyAudioAtmosphere();
    }

    private void UpdateAudioReactiveAtmosphere()
    {
        var easing = _engine.State == EngineState.Playing ? 0.16 : 0.10;
        _visualEnergy = Approach(_visualEnergy, _targetEnergy, easing);
        _visualBass = Approach(_visualBass, _targetBass, easing);
        _visualTreble = Approach(_visualTreble, _targetTreble, easing);

        ApplyAudioAtmosphere();

        if (_engine.State == EngineState.Playing)
            return;

        if (_visualEnergy < 0.003 && _visualBass < 0.003 && _visualTreble < 0.003)
        {
            ResetAudioAtmosphere();
            _atmosphereTimer.Stop();
        }
    }

    private void ApplyAudioAtmosphere()
    {
        var energy = SoftLimit(_visualEnergy);
        var bass = SoftLimit(_visualBass);
        var treble = SoftLimit(_visualTreble);
        var hasArtwork = AppArtworkBackground.IsVisible || PlayerArtworkBackground.IsVisible || ListArtworkBackground.IsVisible;

        AppArtworkBackground.Opacity = AppArtworkBackground.IsVisible ? 0.12 + energy * 0.19 : 0;
        PlayerArtworkBackground.Opacity = PlayerArtworkBackground.IsVisible ? 0.20 + energy * 0.27 : 0;
        ListArtworkBackground.Opacity = ListArtworkBackground.IsVisible ? 0.16 + energy * 0.09 : 0;

        SetScale(AppArtworkBackground, 1.08 + bass * 0.048);
        SetScale(PlayerArtworkBackground, 1.06 + bass * 0.038);
        SetScale(ListArtworkBackground, 1.06 + bass * 0.022);
        SetBlur(AppArtworkBackground, 20 + energy * 8.0);
        SetBlur(PlayerArtworkBackground, 60 + energy * 16.0 + treble * 6.5);
        SetBlur(ListArtworkBackground, 20 + energy * 5.0);

        var red = ToByte(30 + bass * 52 + treble * 88);
        var green = ToByte(154 + energy * 58 + treble * 68);
        var blue = ToByte(240 - bass * 48 + treble * 20);
        _appAtmosphereBrush.Color = hasArtwork
            ? Color.FromArgb(ToByte(energy * 42 + treble * 18), red, green, blue)
            : Colors.Transparent;
        _playerAtmosphereBrush.Color = hasArtwork
            ? Color.FromArgb(ToByte(24 + energy * 82 + bass * 40), red, green, blue)
            : Colors.Transparent;
        _playerTopGlowBrush.Color = hasArtwork
            ? Color.FromArgb(ToByte(42 + energy * 92 + treble * 42), red, green, blue)
            : Colors.Transparent;
        _playerChromeEdgeBrush.Color = hasArtwork
            ? Color.FromArgb(ToByte(28 + energy * 42), red, green, blue)
            : Color.Parse("#30283A46");
    }

    private static double Approach(double current, double target, double amount) =>
        current + (target - current) * amount;

    private static double SoftLimit(double value) =>
        Math.Clamp(1 - Math.Exp(-Math.Max(0, value) * 1.45), 0, 1);

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static void SetScale(Image image, double scale)
    {
        if (image.RenderTransform is ScaleTransform transform)
        {
            transform.ScaleX = scale;
            transform.ScaleY = scale;
        }
    }

    private static void SetBlur(Image image, double radius)
    {
        if (image.Effect is BlurEffect blur)
            blur.Radius = radius;
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

    private void ResumeListeningSession(int trackId, TimeSpan position)
    {
        _listeningTrackId = trackId;
        _lastListeningPositionSeconds = position.TotalSeconds;
        _unflushedListeningSeconds = 0;
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
        if (_nextTrackIndex < 0 || _nextTrackIndex >= _filteredItems.Count)
        {
            UpcomingBar.IsVisible = false;
            return;
        }

        UpcomingBar.IsVisible = true;
        UpcomingTrackText.Text = _filteredItems[_nextTrackIndex].Track.Title;
        CrossfadeStatusText.Text = string.Empty;
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
