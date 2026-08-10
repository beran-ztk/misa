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
using Resona.Core;
using Resona.Models;
using Resona.Services;
using SkiaSharp;

namespace Resona.Views;

public enum LibrarySortBy { Name, Rating }
public enum LibrarySortDirection { Ascending, Descending }

public partial class MusicView : UserControl
{
    // Engine
    private readonly PlaybackEngine _engine = new();
    private readonly GlobalMediaKeyListener _globalMediaKeys = new();
    private readonly WindowsMediaSession _windowsMediaSession = new();
    private readonly DiscordPresenceService _discordPresence = new();
    private readonly DispatcherTimer _atmosphereTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DispatcherTimer _backdropFocusSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly SolidColorBrush _playerTopGlowBrush = new(Colors.Transparent);
    private readonly SolidColorBrush _playerChromeEdgeBrush = new(Color.Parse("#4A756B54"));
    private readonly GradientStop _appAmbientPrimaryStop;
    private readonly GradientStop _appAmbientSecondaryStop;
    private readonly GradientStop _filterAmbientPrimaryStop;
    private readonly GradientStop _filterAmbientSecondaryStop;
    private readonly GradientStop _playerAmbientPrimaryStop;
    private readonly GradientStop _playerAmbientSecondaryStop;
    private static readonly TimeSpan ArtworkTransitionDuration = TimeSpan.FromSeconds(5);
    private static readonly AmbientPalette DefaultAmbientPalette = new(
        Color.Parse("#5865B8"),
        Color.Parse("#8051AE"));
    private Color _ambientPrimary = DefaultAmbientPalette.Primary;
    private Color _ambientSecondary = DefaultAmbientPalette.Secondary;
    private Color _ambientStartPrimary = DefaultAmbientPalette.Primary;
    private Color _ambientStartSecondary = DefaultAmbientPalette.Secondary;
    private Color _targetAmbientPrimary = DefaultAmbientPalette.Primary;
    private Color _targetAmbientSecondary = DefaultAmbientPalette.Secondary;
    private bool _hasArtworkPalette;
    private bool _isSeeking;
    private double _targetEnergy;
    private double _targetBass;
    private double _targetTreble;
    private double _visualEnergy;
    private double _visualBass;
    private double _visualTreble;
    private AppearanceSettings _appearanceSettings = AppearanceSettings.Balanced();

    // Playback settings
    private bool _shuffle;
    private string _loopStatus = "None";
    private LibrarySortBy _sortBy = LibrarySortBy.Name;
    private LibrarySortDirection _sortDirection = LibrarySortDirection.Ascending;
    private bool _updatingSortControls;
    private readonly Dictionary<int, double> _shufflePriorities = [];

    // UI state
    private bool _filterPanelVisible;
    private CancellationTokenSource? _thumbLoadCts;
    private CancellationTokenSource? _toastCts;
    private Bitmap? _playerArtwork;
    private Bitmap? _previousPlayerArtwork;
    private int _playerArtworkTrackId = -1;
    private readonly Dictionary<int, double> _trackBackdropFocus = [];
    private readonly HashSet<int> _pendingBackdropFocusTrackIds = [];
    private bool _isDeletingTrack;
    private DateTimeOffset _artworkTransitionStartedAt;
    private double _artworkTransitionProgress = 1;
    private double _outgoingAppScale = 1.08;
    private double _outgoingPlayerScale = 1.10;
    private double _outgoingAppBlur = 20;
    private double _outgoingPlayerBlur = 30;
    private bool _libraryRefreshPending;

    // Crossfade state
    private int _lastKnownActiveId = -1;
    private bool _crossfadeTriggered;
    private bool _restartQueueFromTopAfterCurrent;
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
    private List<TrackDisplayItem> _visibleItems = [];
    private List<PortableFilterPreset> _filterPresets = [];
    private string? _activeFilterPresetName;
    private bool _isCreatingPreset;
    private bool _showReviewOnly;
    private bool _manualRatingFilter;
    private string _visibilityFilterMode = "All";
    private PlayerSessionSettings _restoredPlayerSession = new();
    private Dictionary<int, int>? _pendingRestoredQueueOrder;
    private int? _pendingRestoredTrackId;
    private bool _restoringPlayerSession = true;
    private bool _suppressPresetAutoSave;
    private bool _suppressSelectionSessionSave;
    private readonly HashSet<string> _selectedRatingNames = new(StringComparer.OrdinalIgnoreCase);
    private MultiSelectFilterControl? _conditionGenreCtrl;
    private MultiSelectFilterControl? _conditionTagCtrl;
    private bool _conditionNegate;
    private FilterSection? _conditionGenreSection;
    private FilterSection? _conditionTagSection;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreCtrl,
        MultiSelectFilterControl TagCtrl,
        bool Negate,
        Action RefreshVisuals);
    private sealed record FilterSection(Control Control, Action Refresh);
    private readonly List<FilterGroupControls> _filterGroups = [];

    public MusicView()
    {
        InitializeComponent();
        MoveFilterDrawerToRootOverlay();
        var appAmbientGradient = (LinearGradientBrush)AppAtmosphereTint.Fill!;
        _appAmbientPrimaryStop = appAmbientGradient.GradientStops[0];
        _appAmbientSecondaryStop = appAmbientGradient.GradientStops[1];
        var filterAmbientGradient = (LinearGradientBrush)FilterAtmosphereTint.Fill!;
        _filterAmbientPrimaryStop = filterAmbientGradient.GradientStops[0];
        _filterAmbientSecondaryStop = filterAmbientGradient.GradientStops[1];
        var playerAmbientGradient = (LinearGradientBrush)PlayerAtmosphereTint.Background!;
        _playerAmbientPrimaryStop = playerAmbientGradient.GradientStops[0];
        _playerAmbientSecondaryStop = playerAmbientGradient.GradientStops[1];
        var appSettings = AppSettingsStore.Load();
        foreach (var pair in appSettings.TrackBackdropFocus)
            _trackBackdropFocus[pair.Key] = pair.Value;
        _appearanceSettings = appSettings.Appearance.Clone().Clamp();
        _restoredPlayerSession = appSettings.PlayerSession;
        _sortBy = Enum.TryParse<LibrarySortBy>(_restoredPlayerSession.SortBy, true, out var restoredSortBy)
            ? restoredSortBy
            : LibrarySortBy.Name;
        _sortDirection = Enum.TryParse<LibrarySortDirection>(_restoredPlayerSession.SortDirection, true, out var restoredDirection)
            ? restoredDirection
            : LibrarySortDirection.Ascending;
        _shuffle = _restoredPlayerSession.ShuffleEnabled;
        _pendingRestoredTrackId = _restoredPlayerSession.ActiveTrackId
                                  ?? _restoredPlayerSession.SelectedTrackId;
        _pendingRestoredQueueOrder = _restoredPlayerSession.QueueTrackIds
            .Distinct()
            .Select((trackId, index) => (trackId, index))
            .ToDictionary(item => item.trackId, item => item.index);
        if (_shuffle)
            foreach (var item in _pendingRestoredQueueOrder)
                _shufflePriorities[item.Key] = item.Value;
        ApplyAppearanceSettings(_appearanceSettings, refreshTrackRows: false);
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        _globalMediaKeys.Pressed += OnGlobalMediaKeyPressed;
        _globalMediaKeys.Start();
        _windowsMediaSession.Pressed += OnGlobalMediaKeyPressed;
        _windowsMediaSession.SeekRequested += OnSystemSeekRequested;
        _windowsMediaSession.PositionRequested += OnSystemPositionRequested;
        _windowsMediaSession.VolumeRequested += OnSystemVolumeRequested;
        _windowsMediaSession.ShuffleRequested += SetShuffle;
        _windowsMediaSession.LoopStatusRequested += OnSystemLoopStatusRequested;
        _windowsMediaSession.OpenUriRequested += OnSystemOpenUriRequested;
        DetachedFromVisualTree += (_, _) =>
        {
            PersistPlayerSession();
            FlushBackdropFocusSave();
            _atmosphereTimer.Stop();
            _engine.Dispose();
            _globalMediaKeys.Dispose();
            _windowsMediaSession.Dispose();
            _discordPresence.Dispose();
            ClearPlayerArtworkBackground(disposeCache: true);
        };

        // Engine events
        _engine.StateChanged += OnEngineStateChanged;
        _engine.TrackNaturallyEnded += OnTrackNaturallyEnded;
        _engine.ProgressUpdated += OnProgressUpdated;
        _engine.AudioLevelUpdated += OnAudioLevelUpdated;
        _atmosphereTimer.Tick += (_, _) => UpdateAudioReactiveAtmosphere();
        _backdropFocusSaveTimer.Tick += (_, _) => FlushBackdropFocusSave();
        PlayerChromeEdge.Background = _playerChromeEdgeBrush;
        PlayerTopGlow.Background = _playerTopGlowBrush;
        InitializeSortControls();

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
        SetVisibilityFilterMode("All", applyFilter: false);
        RefreshCompletionFilterVisuals();
        FileList.SelectionChanged += (_, _) =>
        {
            if (!_suppressSelectionSessionSave)
                PersistPlayerSession();
        };
        PlayerBar.SizeChanged += (_, _) => UpdateSettingsLayout();
        PlayerBar.SizeChanged += (_, _) => UpdateEditorBounds();
        PlayerBar.SizeChanged += (_, _) => UpdateImportBounds();

        // Volume
        Values.Volume = AppSettingsStore.Load().Volume;
        VolumeSlider.Value = Values.Volume * 100.0;
        _engine.ApplyVolume(Values.Volume);
        VolumeSlider.ValueChanged += (_, _) =>
        {
            Values.Volume = (float)VolumeSlider.Value / 100.0f;
            _engine.ApplyVolume(Values.Volume);
            AppSettingsStore.SaveVolume(Values.Volume);
            _windowsMediaSession.UpdateVolume(Values.Volume);
        };
        
        try { MusicLibraryService.Current.Initialize(); }
        catch (Exception ex) { StatusText.Text = $"Database error: {ex.Message}"; StatusText.IsVisible = true; return; }
        var backupResult = DatabaseBackupService.Current.EnsureTodayBackups();
        if (backupResult.Errors.Count > 0)
            ShowToast($"Database backup warning: {backupResult.Errors[0]}");
        ImportQueueService.Current.Initialize();
        BackgroundAnalysisService.Current.Initialize();
        ChannelDownloadService.Current.Initialize();
        UpdateImportBounds();

        LoadLookups();
        InitializeFilterConditionBuilder();
        LoadFilterPresets();
        RebuildFilterConditionsPanel();
        RefreshTrackList();
        _restoringPlayerSession = false;
        UpdateShuffleButton();
        PersistPlayerSession();
        _ = RefreshChannelsOnStartupAsync();

        SettingsOverlay.PreloadGenreVocabulary();

        AddTrackOverlay.TrackDownloaded += warning =>
        {
            AddTrackOverlay.IsVisible = false;
            MarkLibraryRefreshPending();
            ShowToast(warning ?? "Track downloaded; analysis queued");
        };
        AddTrackOverlay.CloseRequested += () => AddTrackOverlay.IsVisible = false;
        ChannelOverlay.CloseRequested += () =>
        {
            StopTrackPreview();
            ChannelOverlay.IsVisible = false;
        };
        ChannelOverlay.ToastRequested += ShowToast;
        ChannelOverlay.PreviewRequested += StartTrackPreview;
        ChannelOverlay.PreviewClosed += StopTrackPreview;
        ChannelOverlay.TrackChanged += trackId =>
        {
            UpdateTrackInList(trackId);
        };
        ImportOverlay.QueueSubmitted += count =>
        {
            ShowToast($"{count} track{(count == 1 ? string.Empty : "s")} added to the import queue");
        };
        ImportOverlay.ToastRequested += ShowToast;
        ImportQueueService.Current.ItemUpdated += _ => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
        });
        ImportQueueService.Current.TrackImported += (track, warning) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            MarkLibraryRefreshPending();
            ShowToast(warning ?? $"Imported: {track.Title}");
        });
        BackgroundAnalysisService.Current.QueueChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
        });
        BackgroundAnalysisService.Current.TrackAnalysisFinished += (track, error) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                MarkLibraryRefreshPending();
                if (ImportOverlay.IsVisible) ImportOverlay.RefreshQueue();
                ShowToast(error is null
                    ? $"Analysis completed: {track.Title}"
                    : error.Contains("cancelled", StringComparison.OrdinalIgnoreCase)
                        ? $"Analysis cancelled: {track.Title}"
                        : $"Analysis failed for {track.Title}: {error}");
                if (error is null && _engine.ActiveTrackId == track.Id)
                    PrepareTrackEditor(track, force: true);
            });
        ChannelDownloadService.Current.QueueChanged += () => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (ChannelOverlay.IsVisible) ChannelOverlay.UpdateDownloadSummary();
        });
        ChannelDownloadService.Current.DownloadFinished += (video, track, error) =>
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                ChannelOverlay.OnDownloadFinished(video.Id, track, error);
                if (track is not null)
                {
                    MarkLibraryRefreshPending();
                }
            });
        EditTrackOverlay.TrackSaved += UpdateTrackInList;
        EditTrackOverlay.PreviewRequested += StartTrackPreview;
        EditTrackOverlay.PreviewClosed += StopTrackPreview;
        EditTrackOverlay.ToastRequested += ShowToast;
        EditTrackOverlay.BackdropFocusChanged += OnEditorBackdropFocusChanged;
        EditTrackOverlay.DeleteRequested += DeleteTrackFromEditorAsync;
        EditTrackOverlay.Closed += PrepareActiveTrackEditor;
        SettingsOverlay.ToastRequested += ShowToast;
        SettingsOverlay.AppearanceChanged += settings => ApplyAppearanceSettings(settings, refreshTrackRows: true);
        SettingsOverlay.LibraryMetadataChanged += RefreshLibraryPresentation;
        SettingsOverlay.ExportRequested += ExportPortableLibrary;
        MusicVideoOverlay.CloseRequested += () => MusicVideoOverlay.IsVisible = false;
        MusicVideoOverlay.ToastRequested += ShowToast;
    }

    private void UpdateEditorBounds()
    {
        // The editor covers the toolbar and library, but deliberately stops above the player.
        EditTrackOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
    }

    private void UpdateImportBounds() =>
        ImportOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);

    private async Task RefreshChannelsOnStartupAsync()
    {
        try
        {
            var added = await MusicLibraryService.Current.RefreshSubscribedChannelsAsync();
            if (added > 0)
                ShowToast($"{added} new channel videos found");
            if (ChannelOverlay.IsVisible)
                ChannelOverlay.RefreshChannels();
        }
        catch
        {
            // Channel refresh is a convenience check; it should not block the music library.
        }
    }

    public void EnableSystemMediaControls()
    {
        _windowsMediaSession.Start();
        _windowsMediaSession.UpdateState(_engine.State);
        _windowsMediaSession.UpdateVolume(Values.Volume);
        _windowsMediaSession.UpdateShuffle(_shuffle);
        _windowsMediaSession.UpdateLoopStatus(_loopStatus);
        Dispatcher.UIThread.Post(() =>
        {
            if (_filteredItems.Count == 0 || _engine.State != EngineState.Stopped)
                return;

            var selectedIndex = GetSelectedFilteredIndex();
            if (selectedIndex < 0)
                selectedIndex = 0;
            EnsureVisibleWindowAround(selectedIndex);
            SetFilteredSelectedIndex(selectedIndex);
            StartPlayback();
            _engine.Pause();
            UpdatePlaybackPositionUi();
            UpdateButtonStates();
        }, DispatcherPriority.Background);
    }

    private void RefreshLibraryPresentation()
    {
        EditTrackOverlay.InvalidateLookups();
        LoadLookups();
        RefreshTrackList();
        PrepareActiveTrackEditor();
    }

    private void MarkLibraryRefreshPending()
    {
        _libraryRefreshPending = true;
        UpdateLibraryRefreshState();
    }

    private void ClearLibraryRefreshPending()
    {
        _libraryRefreshPending = false;
        UpdateLibraryRefreshState();
    }

    private void UpdateLibraryRefreshState()
    {
        LibraryDirtyText.IsVisible = _libraryRefreshPending;
        RefreshLibraryButton.IsVisible = _libraryRefreshPending;
    }

    private void InitializeSortControls()
    {
        _updatingSortControls = true;
        SortByBox.ItemsSource = new[] { "Name", "Rating" };
        SortByBox.SelectedIndex = _sortBy == LibrarySortBy.Rating ? 1 : 0;
        UpdateSortDirectionButton();
        _updatingSortControls = false;
    }

    private void OnSortChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingSortControls)
            return;

        _sortBy = SortByBox.SelectedIndex == 1 ? LibrarySortBy.Rating : LibrarySortBy.Name;
        ApplyFilter();
        PersistPlayerSession();
    }

    private void OnSortDirectionClicked(object? sender, RoutedEventArgs e)
    {
        _sortDirection = _sortDirection == LibrarySortDirection.Ascending
            ? LibrarySortDirection.Descending
            : LibrarySortDirection.Ascending;
        UpdateSortDirectionButton();
        ApplyFilter();
        PersistPlayerSession();
    }

    private void UpdateSortDirectionButton()
    {
        SortDirectionButton.Content = _sortDirection == LibrarySortDirection.Ascending ? "↑" : "↓";
        ToolTip.SetTip(SortDirectionButton,
            _sortDirection == LibrarySortDirection.Ascending ? "Ascending" : "Descending");
    }

    // ─── Track list ──────────────────────────────────────────────────────────

    private void LoadLookups()
    {
        var selectedRatings = _selectedRatingNames.ToList();

        Values.Genres = MusicLibraryService.Current.GetGenres();
        Values.Tags = MusicLibraryService.Current.GetTags();
        Values.Styles = MusicLibraryService.Current.GetStyles();
        Values.Ratings = MusicLibraryService.Current.GetRatings();

        _selectedRatingNames.Clear();
        foreach (var rating in Values.Ratings)
            if (selectedRatings.Contains(rating.Name, StringComparer.OrdinalIgnoreCase))
                _selectedRatingNames.Add(rating.Name);
        RefreshRatingFilterControls();

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
        var previousItems = _allItems.ToDictionary(item => item.Track.Id);

        var tracks = MusicLibraryService.Current.GetTracks();
        var unanalyzedTrackIds = MusicLibraryService.Current.GetTrackIdsMissingAnalysis();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackTagIds = MusicLibraryService.Current.GetAllTrackTagIds();

        var genreMap = Values.Genres.ToDictionary(g => g.Id, g => g.Name);
        var tagMap = Values.Tags.ToDictionary(t => t.Id);
        var tagOrder = Values.Tags.Select((tag, index) => (tag.Id, index))
            .ToDictionary(item => item.Id, item => item.index);
        var ratingMap = Values.Ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = Values.Styles.ToDictionary(s => s.Id, s => s.Name);

        var newItems = new List<TrackDisplayItem>();
        foreach (var track in tracks)
        {
            var needsAnalysis = unanalyzedTrackIds.Contains(track.Id);
            if (previousItems.TryGetValue(track.Id, out var previous)
                && string.Equals(previous.Track.UpdatedAt, track.UpdatedAt, StringComparison.Ordinal)
                && previous.NeedsAnalysis == needsAnalysis)
            {
                previous.IsPlaying = previous.Track.Id == _engine.ActiveTrackId;
                newItems.Add(previous);
                continue;
            }

            var item = CreateTrackDisplayItem(track, needsAnalysis);
            if (previous?.Thumbnail is not null)
            {
                item.Thumbnail = previous.Thumbnail;
                item.SetArtworkPalette(previous.ArtworkPrimaryColor, previous.ArtworkSecondaryColor);
                previous.Thumbnail = null;
            }
            newItems.Add(item);
        }

        foreach (var previous in previousItems.Values)
            if (!newItems.Contains(previous))
                previous.Thumbnail?.Dispose();

        _allItems = newItems;

        ApplyFilter();
        if (_engine.ActiveTrackId >= 0
            && _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track is { } activeTrack)
            UpdatePlayerArtworkBackground(activeTrack);
        ClearLibraryRefreshPending();
    }

    private TrackDisplayItem CreateTrackDisplayItem(MusicTrack track, bool needsAnalysis)
    {
        var genreIds = _allTrackGenreIds.GetValueOrDefault(track.Id, []);
        var tagIds = _allTrackTagIds.GetValueOrDefault(track.Id, []);
        var styleIds = _allTrackStyleIds.GetValueOrDefault(track.Id, []);

        var genreMap = Values.Genres.ToDictionary(g => g.Id, g => g.Name);
        var tagMap = Values.Tags.ToDictionary(t => t.Id);
        var tagOrder = Values.Tags.Select((tag, index) => (tag.Id, index))
            .ToDictionary(item => item.Id, item => item.index);
        var ratingMap = Values.Ratings.ToDictionary(r => r.Id, r => r.Name);
        var styleMap = Values.Styles.ToDictionary(s => s.Id, s => s.Name);

        var modelGenreAssignments = MusicLibraryService.Current.GetTrackModelGenres(track.Id)
            .Where(assignment => assignment.IsEnabled)
            .ToList();
        var modelGenreStr = string.Join(", ", modelGenreAssignments
            .Where(assignment => !assignment.IsManual)
            .Select(assignment => ShortGenreName(assignment.GenreName))
            .Where(name => name.Length > 0)
            .Order());
        var manualGenreStr = string.Join(", ", modelGenreAssignments
            .Where(assignment => assignment.IsManual)
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
                CategoryBrush(null)))
            .ToList();
        var ratingName = track.RatingId is int ratingId ? ratingMap.GetValueOrDefault(ratingId, "") : "None";
        var durationText = track.DurationSeconds.HasValue ? FormatDuration(track.DurationSeconds.Value) : "";

        var item = new TrackDisplayItem(track, genreStr, modelGenreStr, manualGenreStr, styleStr, durationText, ratingName, tagDisplays, track.ChannelName ?? "")
        {
            NeedsReview = track.NeedsReview,
            NeedsAnalysis = needsAnalysis,
            IsPlaying = track.Id == _engine.ActiveTrackId
        };
        item.ApplyAppearance(_appearanceSettings);
        return item;
    }

    private void UpdateTrackInList(int trackId)
    {
        var selectedTrackId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id;
        var previous = _allItems.FirstOrDefault(item => item.Track.Id == trackId);
        var updatedTrack = MusicLibraryService.Current.GetTrackById(trackId);
        if (updatedTrack is null)
        {
            RefreshTrackList();
            return;
        }

        _allTrackStyleIds[trackId] = MusicLibraryService.Current.GetTrackStyleIds(trackId);
        _allTrackGenreIds[trackId] = MusicLibraryService.Current.GetTrackGenreIds(trackId);
        _allTrackTagIds[trackId] = MusicLibraryService.Current.GetTrackTagIds(trackId);

        var needsAnalysis = MusicLibraryService.Current.GetTrackAudioAnalysis(trackId) is null;
        var updatedItem = CreateTrackDisplayItem(updatedTrack, needsAnalysis);
        if (previous?.Thumbnail is not null)
        {
            updatedItem.Thumbnail = previous.Thumbnail;
            updatedItem.SetArtworkPalette(previous.ArtworkPrimaryColor, previous.ArtworkSecondaryColor);
            previous.Thumbnail = null;
        }

        var allIndex = _allItems.FindIndex(item => item.Track.Id == trackId);
        if (allIndex >= 0)
            _allItems[allIndex] = updatedItem;
        else
            _allItems.Insert(0, updatedItem);

        var filteredIndex = _filteredItems.FindIndex(item => item.Track.Id == trackId);
        if (filteredIndex >= 0)
            _filteredItems[filteredIndex] = updatedItem;

        if (_engine.ActiveTrackId == trackId)
        {
            NowPlayingText.Text = updatedTrack.Title;
            UpdateDiscordPresence();
            UpdatePlayerArtworkBackground(updatedTrack);
        }

        if (filteredIndex >= 0)
            EnsureVisibleWindowAround(filteredIndex);

        RefreshVisibleItemsSource(selectedTrackId);
        UpdatePlaylistSummary();
        RefreshNextTrackPreview();
        UpdateReviewFilterButton();
        RestartVisibleThumbnailLoad();
    }

    private List<FilterGroup> CurrentFilterGroups() =>
        _filterGroups
            .Select(fg => new FilterGroup(
                SelectedIds(fg.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id),
                new HashSet<int>(),
                SelectedIds(fg.TagCtrl.SelectedItems, Values.Tags, TagFilterName, t => t.Id),
                fg.Negate))
            .ToList();

    private static string ShortGenreName(string genreName)
    {
        var separator = genreName.LastIndexOf('→');
        return separator >= 0 && separator + 1 < genreName.Length
            ? genreName[(separator + 1)..].Trim()
            : genreName;
    }

    private static string TagFilterName(Tag tag) => tag.Name;

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
            tag.Name));

    private static IBrush CategoryBrush(string? color)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? "#C7E59F" : color)); }
        catch { return new SolidColorBrush(Color.Parse("#C7E59F")); }
    }

    private static IBrush SafeBrush(string? color, string fallback)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? fallback : color)); }
        catch { return new SolidColorBrush(Color.Parse(fallback)); }
    }

    private async Task LoadThumbnailsAsync(CancellationToken ct)
    {
        var items = _visibleItems.ToList();

        Dictionary<int, LoadedTrackThumbnail?> artworkByTrackId;
        try
        {
            artworkByTrackId = await Task.Run(() =>
            {
                var result = new Dictionary<int, LoadedTrackThumbnail?>();
                foreach (var item in items)
                {
                    ct.ThrowIfCancellationRequested();
                    var artwork = MusicLibraryService.Current.GetTrackThumbnail(item.Track.Id);
                    result[item.Track.Id] = artwork is { Length: > 0 }
                        ? new LoadedTrackThumbnail(artwork, ExtractAmbientPalette(artwork))
                        : null;
                }
                return result;
            }, ct);
        }
        catch (OperationCanceledException) { return; }

        if (ct.IsCancellationRequested) return;

        bool any = false;
        foreach (var item in items)
        {
            if (item.Thumbnail is not null)
                continue;

            if (artworkByTrackId.TryGetValue(item.Track.Id, out var loaded) && loaded is not null)
            {
                try
                {
                    using var stream = new MemoryStream(loaded.Artwork);
                    item.Thumbnail = new Bitmap(stream);
                    item.SetArtworkPalette(loaded.Palette.Primary, loaded.Palette.Secondary);
                    any = true;
                }
                catch { }
            }
        }

        if (!any || ct.IsCancellationRequested) return;

        RefreshVisibleItemsSource((FileList.SelectedItem as TrackDisplayItem)?.Track.Id);
    }

    private void ApplyFilter()
    {
        var previousSuppression = _suppressSelectionSessionSave;
        _suppressSelectionSessionSave = true;
        try
        {
            ApplyFilterCore();
        }
        finally
        {
            _suppressSelectionSessionSave = previousSuppression;
        }
    }

    private void ApplyFilterCore()
    {
        var selRatingIds = SelectedRatingIds();
        var selVisibility = SelectedVisibility();
        var itemById = _allItems.ToDictionary(i => i.Track.Id);

        var groups = CurrentFilterGroups();
        
        var filtered = TrackFilter.Apply(
            _allItems.Select(i => i.Track),
            _allTrackGenreIds,
            _allTrackStyleIds,
            _allTrackTagIds,
            selRatingIds,
            selVisibility,
            groups,
            SearchBox.Text);

        if (_manualRatingFilter && selRatingIds.Count == 0)
            filtered.Clear();

        _filteredItems = filtered
            .Where(t => itemById.ContainsKey(t.Id))
            .Select(t => itemById[t.Id])
            .ToList();

        if (ExcludeNeedsReviewCheckBox.IsChecked == true)
            _filteredItems = _filteredItems
                .Where(item => !item.NeedsReview)
                .ToList();

        if (ExcludeNeedsAnalysisCheckBox.IsChecked == true)
            _filteredItems = _filteredItems
                .Where(item => !item.NeedsAnalysis)
                .ToList();

        if (_showReviewOnly)
            _filteredItems = _filteredItems
                .Where(item => item.NeedsReview)
                .ToList();

        ApplyLibrarySort();

        if (_shuffle)
            ShuffleFilteredItems();

        RestoreSavedQueueOrder();

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        var selectedTrackId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id;

        RefreshVisibleItemsSource(selectedTrackId);
        RestoreOrInitializeSelection(selectedTrackId);
        UpdatePlaylistSummary();
        RefreshNextTrackPreview();
        UpdateFilterCounts();
        UpdateReviewFilterButton();
        RestartVisibleThumbnailLoad();
    }

    private void ApplyLibrarySort()
    {
        var ratingSortById = Values.Ratings.ToDictionary(rating => rating.Id, rating => rating.SortOrder);
        IOrderedEnumerable<TrackDisplayItem> sorted = _sortBy switch
        {
            LibrarySortBy.Rating when _sortDirection == LibrarySortDirection.Ascending =>
                _filteredItems
                    .OrderBy(item => item.Track.RatingId is int ratingId
                        ? ratingSortById.GetValueOrDefault(ratingId, int.MaxValue)
                        : int.MaxValue)
                    .ThenBy(item => item.Track.Title, StringComparer.OrdinalIgnoreCase),
            LibrarySortBy.Rating =>
                _filteredItems
                    .OrderByDescending(item => item.Track.RatingId is int ratingId
                        ? ratingSortById.GetValueOrDefault(ratingId, int.MinValue)
                        : int.MinValue)
                    .ThenBy(item => item.Track.Title, StringComparer.OrdinalIgnoreCase),
            LibrarySortBy.Name when _sortDirection == LibrarySortDirection.Descending =>
                _filteredItems.OrderByDescending(item => item.Track.Title, StringComparer.OrdinalIgnoreCase),
            _ =>
                _filteredItems.OrderBy(item => item.Track.Title, StringComparer.OrdinalIgnoreCase)
        };

        _filteredItems = sorted.ToList();
    }

    private void RestoreOrInitializeSelection(int? previousSelectedTrackId)
    {
        if (_filteredItems.Count == 0)
        {
            SetFilteredSelectedIndex(-1);
            return;
        }

        var targetTrackId = _engine.ActiveTrackId >= 0
            ? _engine.ActiveTrackId
            : previousSelectedTrackId ?? _pendingRestoredTrackId;

        if (targetTrackId is int id)
        {
            var index = _filteredItems.FindIndex(item => item.Track.Id == id);
            if (index >= 0)
            {
                EnsureVisibleWindowAround(index);
                SetFilteredSelectedIndex(index);
                _pendingRestoredTrackId = null;
                return;
            }
        }

        if (FileList.SelectedIndex < 0 || FileList.SelectedIndex >= _visibleItems.Count)
            SetFilteredSelectedIndex(0);

        if (FileList.SelectedIndex >= 0 && FileList.SelectedIndex < _visibleItems.Count)
            FileList.ScrollIntoView(_visibleItems[FileList.SelectedIndex]);
    }

    private void RefreshVisibleItemsSource(int? selectedTrackId = null)
    {
        selectedTrackId ??= (FileList.SelectedItem as TrackDisplayItem)?.Track.Id;
        _visibleItems = _filteredItems.ToList();

        FileList.ItemsSource = _visibleItems;
        if (selectedTrackId is int id)
        {
            var index = _visibleItems.FindIndex(item => item.Track.Id == id);
            if (index >= 0)
                FileList.SelectedIndex = index;
        }
    }

    private void EnsureVisibleWindowAround(int filteredIndex)
    {
        if (filteredIndex < 0 || filteredIndex >= _filteredItems.Count)
            return;

        if (filteredIndex < _visibleItems.Count)
            FileList.ScrollIntoView(_visibleItems[filteredIndex]);
    }

    private void RestoreSavedQueueOrder()
    {
        if (_pendingRestoredQueueOrder is null || _filteredItems.Count == 0)
            return;

        var fallbackOrder = _filteredItems
            .Select((item, index) => (item.Track.Id, index))
            .ToDictionary(item => item.Id, item => item.index);
        var savedCount = _pendingRestoredQueueOrder.Count;

        _filteredItems = _filteredItems
            .OrderBy(item => _pendingRestoredQueueOrder.TryGetValue(item.Track.Id, out var savedIndex)
                ? savedIndex
                : savedCount + fallbackOrder[item.Track.Id])
            .ToList();

        if (_shuffle)
        {
            _shufflePriorities.Clear();
            for (var index = 0; index < _filteredItems.Count; index++)
                _shufflePriorities[_filteredItems[index].Track.Id] = index;
        }

        _pendingRestoredQueueOrder = null;
    }

    private void PersistPlayerSession()
    {
        if (_restoringPlayerSession)
            return;

        AppSettingsStore.SavePlayerSession(new PlayerSessionSettings
        {
            ActiveFilterPresetName = _activeFilterPresetName,
            ActiveTrackId = _engine.ActiveTrackId >= 0 ? _engine.ActiveTrackId : null,
            SelectedTrackId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id,
            ShuffleEnabled = _shuffle,
            SortBy = _sortBy.ToString(),
            SortDirection = _sortDirection.ToString(),
            QueueTrackIds = _filteredItems.Select(item => item.Track.Id).ToList()
        });
    }

    private int GetSelectedFilteredIndex()
    {
        if (FileList.SelectedItem is not TrackDisplayItem selected)
            return -1;

        return _filteredItems.FindIndex(item => item.Track.Id == selected.Track.Id);
    }

    private void SetFilteredSelectedIndex(int filteredIndex)
    {
        FileList.SelectedIndex = filteredIndex >= 0 && filteredIndex < _visibleItems.Count
            ? filteredIndex
            : -1;
    }

    private void RestartVisibleThumbnailLoad()
    {
        _thumbLoadCts?.Cancel();
        _thumbLoadCts = new CancellationTokenSource();
        _ = LoadThumbnailsAsync(_thumbLoadCts.Token);
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
        var selectedRatingIds = SelectedRatingIds();
        var selectedGenreIds = SelectedIds(group.GenreCtrl.SelectedItems, Values.Genres, g => g.Name, g => g.Id);
        var selectedTagIds = SelectedIds(group.TagCtrl.SelectedItems, Values.Tags, TagFilterName, t => t.Id);
        var term = SearchBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(track => track.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (_manualRatingFilter && selectedRatingIds.Count == 0)
            return [];

        if (selectedRatingIds.Count > 0)
            query = query.Where(track => track.RatingId is int ratingId && selectedRatingIds.Contains(ratingId));

        var selectedVisibility = SelectedVisibility();
        if (selectedVisibility.Count > 0)
            query = query.Where(track => selectedVisibility.Contains(track.IsPublic));

        if (ExcludeNeedsReviewCheckBox.IsChecked == true)
        {
            var reviewTrackIds = _allItems
                .Where(item => item.NeedsReview)
                .Select(item => item.Track.Id)
                .ToHashSet();
            query = query.Where(track => !reviewTrackIds.Contains(track.Id));
        }

        if (ExcludeNeedsAnalysisCheckBox.IsChecked == true)
        {
            var unanalyzedTrackIds = _allItems
                .Where(item => item.NeedsAnalysis)
                .Select(item => item.Track.Id)
                .ToHashSet();
            query = query.Where(track => !unanalyzedTrackIds.Contains(track.Id));
        }

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

    private HashSet<int> SelectedRatingIds() =>
        !_manualRatingFilter
            ? []
            : Values.Ratings
                .Where(rating => _selectedRatingNames.Contains(rating.Name))
                .Select(rating => rating.Id)
                .ToHashSet();

    private void OnAllRatingsPressed(object? sender, PointerPressedEventArgs e)
    {
        SetRatingFilterMode(manual: false);
        e.Handled = true;
    }

    private void OnManualRatingsPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_manualRatingFilter)
            SetRatingFilterMode(manual: true);
        e.Handled = true;
    }

    private void SetRatingFilterMode(
        bool manual,
        IEnumerable<string>? selectedRatings = null,
        bool applyFilter = true)
    {
        _manualRatingFilter = manual;
        _selectedRatingNames.Clear();
        if (manual && selectedRatings is not null)
        {
            var available = Values.Ratings
                .Select(rating => rating.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var rating in selectedRatings)
                if (available.Contains(rating))
                    _selectedRatingNames.Add(rating);
        }

        RefreshRatingFilterControls();
        if (applyFilter)
            ApplyFilterDefinitionChange();
    }

    private void RefreshRatingFilterControls()
    {
        RatingButtonsPanel.IsVisible = _manualRatingFilter;
        if (RatingModeIndicator.RenderTransform is TranslateTransform transform)
            transform.X = _manualRatingFilter ? 69 : 0;
        RatingModeIndicator.CornerRadius = _manualRatingFilter
            ? new CornerRadius(0, 5, 5, 0)
            : new CornerRadius(5, 0, 0, 5);
        AllRatingsText.Foreground = ThemeResources.Brush(_manualRatingFilter
            ? "Theme.Brush.TextMuted"
            : "Theme.Brush.TextStrong");
        ManualRatingsText.Foreground = ThemeResources.Brush(_manualRatingFilter
            ? "Theme.Brush.TextStrong"
            : "Theme.Brush.TextMuted");

        RatingButtonsPanel.Children.Clear();
        if (!_manualRatingFilter)
            return;

        foreach (var rating in Values.Ratings.OrderBy(rating => rating.SortOrder))
        {
            var selected = _selectedRatingNames.Contains(rating.Name);
            var accent = RatingAccentColor(rating.Name);
            var button = new Button
            {
                Content = rating.Name,
                Height = 31,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Background = selected
                    ? new SolidColorBrush(Color.FromArgb(76, accent.R, accent.G, accent.B))
                    : ThemeResources.Brush("Theme.Brush.Surface"),
                BorderBrush = selected
                    ? new SolidColorBrush(Color.FromArgb(210, accent.R, accent.G, accent.B))
                    : ThemeResources.Brush("Theme.Brush.BorderSubtle"),
                Foreground = selected
                    ? new SolidColorBrush(RatingForegroundColor(rating.Name))
                    : ThemeResources.Brush("Theme.Brush.TextMuted"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Opacity = selected ? 1 : 0.62,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            button.Click += (_, _) =>
            {
                if (!_selectedRatingNames.Add(rating.Name))
                    _selectedRatingNames.Remove(rating.Name);
                RefreshRatingFilterControls();
                ApplyFilterDefinitionChange();
            };
            RatingButtonsPanel.Children.Add(button);
        }
    }

    private static Color RatingAccentColor(string ratingName) => ratingName switch
    {
        "Favorite" => Color.FromRgb(235, 194, 83),
        "Great" => Color.FromRgb(83, 190, 108),
        "Good" => Color.FromRgb(71, 177, 150),
        "Okay" => Color.FromRgb(151, 156, 116),
        RatingNames.Avoid => Color.FromRgb(211, 78, 65),
        _ => Color.FromRgb(205, 148, 67)
    };

    private static Color RatingForegroundColor(string ratingName) => ratingName switch
    {
        "Favorite" => Color.FromRgb(255, 230, 150),
        "Great" => Color.FromRgb(188, 242, 185),
        "Good" => Color.FromRgb(176, 232, 212),
        "Okay" => Color.FromRgb(226, 224, 194),
        RatingNames.Avoid => Color.FromRgb(246, 175, 160),
        _ => Color.FromRgb(243, 203, 128)
    };

    private HashSet<bool> SelectedVisibility()
    {
        return _visibilityFilterMode switch
        {
            "Public" => [true],
            "Private" => [false],
            _ => []
        };
    }

    private void OnAllVisibilityPressed(object? sender, PointerPressedEventArgs e)
    {
        SetVisibilityFilterMode("All");
        e.Handled = true;
    }

    private void OnPublicVisibilityPressed(object? sender, PointerPressedEventArgs e)
    {
        SetVisibilityFilterMode("Public");
        e.Handled = true;
    }

    private void OnPrivateVisibilityPressed(object? sender, PointerPressedEventArgs e)
    {
        SetVisibilityFilterMode("Private");
        e.Handled = true;
    }

    private void SetVisibilityFilterMode(string mode, bool applyFilter = true)
    {
        _visibilityFilterMode = mode is "Public" or "Private" ? mode : "All";
        var index = _visibilityFilterMode switch { "Public" => 1, "Private" => 2, _ => 0 };

        var transparent = new SolidColorBrush(Colors.Transparent);
        AllVisibilityButton.Background = index == 0 ? ThemeResources.Brush("Theme.Brush.AccentSurface") : transparent;
        PublicVisibilityButton.Background = index == 1 ? ThemeResources.Brush("Theme.Brush.AccentSurface") : transparent;
        PrivateVisibilityButton.Background = index == 2 ? ThemeResources.Brush("Theme.Brush.AccentSurface") : transparent;

        AllVisibilityText.Foreground = ThemeResources.Brush(index == 0 ? "Theme.Brush.TextStrong" : "Theme.Brush.TextMuted");
        PublicVisibilityText.Foreground = ThemeResources.Brush(index == 1 ? "Theme.Brush.TextStrong" : "Theme.Brush.TextMuted");
        PrivateVisibilityText.Foreground = ThemeResources.Brush(index == 2 ? "Theme.Brush.TextStrong" : "Theme.Brush.TextMuted");

        if (applyFilter)
            ApplyFilterDefinitionChange();
    }

    private void OnCompletionFilterChanged(object? sender, RoutedEventArgs e)
    {
        RefreshCompletionFilterVisuals();
        ApplyFilterDefinitionChange();
    }

    private void RefreshCompletionFilterVisuals()
    {
        var reviewSelected = ExcludeNeedsReviewCheckBox.IsChecked == true;
        ExcludeNeedsReviewCheckBox.Background = reviewSelected ? Brush("#18FFD27A") : Brushes.Transparent;
        ExcludeNeedsReviewCheckBox.BorderBrush = reviewSelected
            ? Brush("#78FFD27A")
            : ThemeResources.Brush("Theme.Brush.BorderSubtle");

        var analysisSelected = ExcludeNeedsAnalysisCheckBox.IsChecked == true;
        ExcludeNeedsAnalysisCheckBox.Background = analysisSelected ? Brush("#18EE5C5C") : Brushes.Transparent;
        ExcludeNeedsAnalysisCheckBox.BorderBrush = analysisSelected
            ? Brush("#78EE5C5C")
            : ThemeResources.Brush("Theme.Brush.BorderSubtle");
    }

    // ─── Toolbar / filter panel ───────────────────────────────────────────────

    private void OnToggleFiltersClicked(object? sender, RoutedEventArgs e)
    {
        _filterPanelVisible = !_filterPanelVisible;
        UpdateSettingsLayout();
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

    private void OnSearchBoxLostFocus(object? sender, RoutedEventArgs e) =>
        Dispatcher.UIThread.Post(UpdateSearchVisibility, DispatcherPriority.Background);

    private void UpdateSearchVisibility()
    {
        var hasSearch = !string.IsNullOrWhiteSpace(SearchBox.Text);
        if (SearchToggleBtn.IsKeyboardFocusWithin)
            return;

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

    private void LoadFilterPresets()
    {
        _filterPresets = FilterPresetStore.Load();
        var restoredPreset = _filterPresets.FirstOrDefault(preset =>
            string.Equals(
                preset.Name,
                _restoredPlayerSession.ActiveFilterPresetName,
                StringComparison.OrdinalIgnoreCase));

        _activeFilterPresetName = restoredPreset?.Name;
        if (restoredPreset is not null)
            ApplyFilterPreset(restoredPreset);
        else
            RebuildPresetRows();
    }

    private void RebuildPresetRows()
    {
        PresetRows.Children.Clear();

        if (_isCreatingPreset)
            PresetRows.Children.Add(CreateNewPresetRow());

        foreach (var preset in _filterPresets.OrderBy(preset => preset.Name, StringComparer.OrdinalIgnoreCase))
            PresetRows.Children.Add(CreatePresetCard(preset));

        AddPresetButton.IsEnabled = !_isCreatingPreset;
    }

    private Control CreatePresetCard(PortableFilterPreset preset)
    {
        var isSelected = string.Equals(preset.Name, _activeFilterPresetName, StringComparison.OrdinalIgnoreCase);
        var title = new TextBlock
        {
            Text = preset.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 12,
            Foreground = isSelected
                ? Brush("#D7E7FF")
                : ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var summary = new TextBlock
        {
            Text = PresetSummary(preset),
            FontSize = 10,
            Opacity = 0.55,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var content = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 28, 0) };
        content.Children.Add(title);
        content.Children.Add(summary);

        var card = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = isSelected
                ? Brush("#303E6591")
                : ThemeResources.Brush("Theme.Brush.Surface"),
            BorderBrush = isSelected
                ? Brush("#A078A9E6")
                : ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10, 8),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        card.Click += (_, _) => SelectFilterPreset(preset.Name);

        var deleteButton = new Button
        {
            Content = "×",
            Width = 26,
            Height = 26,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 5, 0),
            FontSize = 15,
            Opacity = 0.58,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(deleteButton, $"Delete {preset.Name}");
        deleteButton.Click += (_, _) => DeleteFilterPreset(preset.Name);

        var container = new Grid();
        container.Children.Add(card);
        container.Children.Add(deleteButton);
        return container;
    }

    private Control CreateNewPresetRow()
    {
        var nameBox = new TextBox
        {
            Watermark = "Preset name",
            Height = 34,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        nameBox.Classes.Add("theme-input");
        nameBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;

            CommitNewPreset(nameBox.Text);
            e.Handled = true;
        };

        var saveButton = new Button
        {
            Content = "Save",
            Background = ThemeResources.Brush("Theme.Brush.AccentSurface"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.Accent"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6),
            FontSize = 11
        };
        saveButton.Click += (_, _) => CommitNewPreset(nameBox.Text);

        var cancelButton = new Button
        {
            Content = "Cancel",
            Background = Brushes.Transparent,
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderStrong"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(10, 6),
            FontSize = 11,
            Opacity = 0.75
        };
        cancelButton.Click += (_, _) =>
        {
            _isCreatingPreset = false;
            RebuildPresetRows();
        };

        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8 };
        actions.Children.Add(saveButton);
        actions.Children.Add(cancelButton);
        Grid.SetColumn(cancelButton, 1);

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(actions);

        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(10)
        };
        border.Child = panel;

        Dispatcher.UIThread.Post(() => nameBox.Focus());
        return border;
    }

    private void OnAddPresetClicked(object? sender, RoutedEventArgs e)
    {
        _isCreatingPreset = true;
        RebuildPresetRows();
    }

    private void CommitNewPreset(string? rawName)
    {
        var name = rawName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        name = UniquePresetName(name);
        var preset = CreatePreset(name);
        _filterPresets.Add(preset);

        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();
        _activeFilterPresetName = preset.Name;
        _isCreatingPreset = false;
        RebuildPresetRows();
        PersistPlayerSession();
    }

    private void SelectFilterPreset(string presetName)
    {
        var preset = _filterPresets.FirstOrDefault(p =>
            string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));

        if (preset is null)
            return;

        _activeFilterPresetName = preset.Name;
        _isCreatingPreset = false;
        RebuildPresetRows();
        ApplyFilterPreset(preset);
        PersistPlayerSession();
    }

    private void SaveActivePresetFromCurrentFilters()
    {
        if (_suppressPresetAutoSave || _activeFilterPresetName is null)
            return;

        var preset = CreatePreset(_activeFilterPresetName);
        var index = _filterPresets.FindIndex(existing =>
            string.Equals(existing.Name, preset.Name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
            return;

        _filterPresets[index] = preset;
        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();
        _activeFilterPresetName = preset.Name;
        RebuildPresetRows();
        PersistPlayerSession();
    }

    private void ApplyFilterDefinitionChange()
    {
        ApplyFilter();
        if (_suppressPresetAutoSave)
            return;
        SaveActivePresetFromCurrentFilters();
        PersistPlayerSession();
    }

    private void DeleteFilterPreset(string presetName)
    {
        var wasSelected = string.Equals(
            presetName,
            _activeFilterPresetName,
            StringComparison.OrdinalIgnoreCase);

        _filterPresets.RemoveAll(preset =>
            string.Equals(preset.Name, presetName, StringComparison.OrdinalIgnoreCase));

        FilterPresetStore.Save(_filterPresets);
        _filterPresets = FilterPresetStore.Load();

        if (!wasSelected)
        {
            RebuildPresetRows();
            PersistPlayerSession();
            return;
        }

        _activeFilterPresetName = null;
        RebuildPresetRows();
        SetRatingFilterMode(manual: false, applyFilter: false);
        SetVisibilityFilterMode("All", applyFilter: false);
        ExcludeNeedsReviewCheckBox.IsChecked = false;
        ExcludeNeedsAnalysisCheckBox.IsChecked = false;
        _filterGroups.Clear();
        RebuildFilterConditionsPanel();
        ClearConditionBuilder();
        ApplyFilter();
        PersistPlayerSession();
    }

    private PortableFilterPreset CreatePreset(string name)
    {
        var groups = _filterGroups
            .Select(group => new PortableFilterGroup(
                SortedNames(group.GenreCtrl.SelectedItems),
                new List<string>(),
                SortedNames(group.TagCtrl.SelectedItems),
                group.Negate))
            .Where(group => group.Genres.Count > 0 || (group.Tags?.Count ?? 0) > 0)
            .ToList();

        return new PortableFilterPreset(
            name,
            _manualRatingFilter ? SortedNames(_selectedRatingNames) : [],
            groups,
            ExcludeNeedsReviewCheckBox.IsChecked == true,
            ExcludeNeedsAnalysisCheckBox.IsChecked == true,
            _manualRatingFilter,
            _visibilityFilterMode);
    }

    private void ApplyFilterPreset(PortableFilterPreset preset)
    {
        _suppressPresetAutoSave = true;
        try
        {
            SetRatingFilterMode(
                preset.ManualRatings || preset.Ratings.Count > 0,
                preset.Ratings,
                applyFilter: false);
            SetVisibilityFilterMode(preset.Visibility, applyFilter: false);
            ExcludeNeedsReviewCheckBox.IsChecked = preset.ExcludeNeedsReview;
            ExcludeNeedsAnalysisCheckBox.IsChecked = preset.ExcludeNeedsAnalysis;

            _filterGroups.Clear();

            var groups = preset.Groups
                .Where(group => group.Genres.Count > 0 || (group.Tags?.Count ?? 0) > 0)
                .ToList();

            foreach (var group in groups)
                _filterGroups.Add(CreateFilterCondition(group.Genres, group.Tags ?? new List<string>(), group.Negate));

            RebuildFilterConditionsPanel();
            ClearConditionBuilder();
        }
        finally
        {
            _suppressPresetAutoSave = false;
        }

        RebuildPresetRows();
        ApplyFilter();
    }

    private string UniquePresetName(string name)
    {
        if (_filterPresets.All(preset => !string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase)))
            return name;

        var suffix = 2;
        string candidate;
        do
        {
            candidate = $"{name} {suffix++}";
        }
        while (_filterPresets.Any(preset => string.Equals(preset.Name, candidate, StringComparison.OrdinalIgnoreCase)));

        return candidate;
    }

    private static string PresetSummary(PortableFilterPreset preset)
    {
        var parts = new List<string>();
        if (preset.ManualRatings && preset.Ratings.Count == 0)
            parts.Add("0 ratings");
        else if (preset.Ratings.Count > 0)
            parts.Add($"{preset.Ratings.Count} rating{(preset.Ratings.Count == 1 ? "" : "s")}");
        if (preset.Groups.Count > 0)
            parts.Add($"{preset.Groups.Count} condition{(preset.Groups.Count == 1 ? "" : "s")}");
        if (!string.Equals(preset.Visibility, "All", StringComparison.OrdinalIgnoreCase))
            parts.Add(preset.Visibility.ToLowerInvariant());
        if (preset.ExcludeNeedsReview)
            parts.Add("no review tracks");
        if (preset.ExcludeNeedsAnalysis)
            parts.Add("analyzed only");

        return parts.Count > 0 ? string.Join(" · ", parts) : "Empty preset";
    }

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

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
        ApplyFilterDefinitionChange();
    }

    private void InitializeFilterConditionBuilder()
    {
        _conditionGenreCtrl = new MultiSelectFilterControl { Placeholder = "All genres" };
        _conditionGenreCtrl.SetItems(GenreFilterOptions());
        _conditionGenreCtrl.SelectionChanged += (_, _) => UpdateCurrentSetSummary();

        _conditionTagCtrl = new MultiSelectFilterControl { Placeholder = "All tags" };
        _conditionTagCtrl.SetItems(TagFilterOptions());
        _conditionTagCtrl.SelectionChanged += (_, _) => UpdateCurrentSetSummary();

        _conditionGenreSection = CreateGenreFilterSection(_conditionGenreCtrl);
        _conditionTagSection = CreateTagFilterSection(_conditionTagCtrl);

        FilterBuilderPanel.Children.Clear();
        FilterBuilderPanel.Children.Add(_conditionGenreSection.Control);
        FilterBuilderPanel.Children.Add(_conditionTagSection.Control);
        SetConditionMode(exclude: false);
        UpdateCurrentSetSummary();
    }

    private void AddConditionFromBuilder()
    {
        if (_conditionGenreCtrl is null || _conditionTagCtrl is null)
            return;

        var selectedGenres = SortedNames(_conditionGenreCtrl.SelectedItems);
        var selectedTags = SortedNames(_conditionTagCtrl.SelectedItems);
        if (selectedGenres.Count == 0 && selectedTags.Count == 0)
            return;

        _filterGroups.Add(CreateFilterCondition(selectedGenres, selectedTags, _conditionNegate));
        RebuildFilterConditionsPanel();
        ClearConditionBuilder();
    }

    private FilterGroupControls CreateFilterCondition(IEnumerable<string> genres, IEnumerable<string> tags, bool negate = false)
    {
        var genreCtrl = new MultiSelectFilterControl { Placeholder = "All genres" };
        genreCtrl.SetItems(GenreFilterOptions());
        genreCtrl.SetSelectedItems(genres, notify: false);

        var tagCtrl = new MultiSelectFilterControl { Placeholder = "All tags" };
        tagCtrl.SetItems(TagFilterOptions());
        tagCtrl.SetSelectedItems(tags, notify: false);

        return new FilterGroupControls(genreCtrl, tagCtrl, negate, () => { });
    }

    private void ClearConditionBuilder()
    {
        _conditionGenreCtrl?.SetSelectedItems(Array.Empty<string>(), notify: false);
        _conditionTagCtrl?.SetSelectedItems(Array.Empty<string>(), notify: false);
        SetConditionMode(exclude: false);
        RefreshConditionBuilder();
    }

    private void OnIncludeConditionPressed(object? sender, PointerPressedEventArgs e)
    {
        SetConditionMode(exclude: false);
        e.Handled = true;
    }

    private void OnExcludeConditionPressed(object? sender, PointerPressedEventArgs e)
    {
        SetConditionMode(exclude: true);
        e.Handled = true;
    }

    private void SetConditionMode(bool exclude)
    {
        _conditionNegate = exclude;
        if (ConditionModeIndicator.RenderTransform is TranslateTransform transform)
            transform.X = exclude ? 59 : 0;
        ConditionModeIndicator.Background = exclude
            ? ThemeResources.Brush("Theme.Brush.DangerSurface")
            : ThemeResources.Brush("Theme.Brush.Success");
        ConditionModeIndicator.CornerRadius = exclude
            ? new CornerRadius(0, 5, 5, 0)
            : new CornerRadius(5, 0, 0, 5);
        IncludeConditionText.Foreground = ThemeResources.Brush(exclude
            ? "Theme.Brush.TextMuted"
            : "Theme.Brush.TextStrong");
        ExcludeConditionText.Foreground = ThemeResources.Brush(exclude
            ? "Theme.Brush.TextStrong"
            : "Theme.Brush.TextMuted");
    }

    private void RefreshConditionBuilder()
    {
        _conditionGenreSection?.Refresh();
        _conditionTagSection?.Refresh();
        UpdateCurrentSetSummary();
    }

    private void UpdateCurrentSetSummary()
    {
        var genres = _conditionGenreCtrl?.SelectedItems
            .Select(DisplayGenreFilterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        var tags = _conditionTagCtrl?.SelectedItems
            .Select(DisplayTagFilterName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        CurrentSetGenresRow.IsVisible = genres.Count > 0;
        CurrentSetTagsRow.IsVisible = tags.Count > 0;
        CurrentSetGenresText.Text = FormatConditionValues(genres);
        CurrentSetTagsText.Text = FormatConditionValues(tags);

        var hasSelection = genres.Count > 0 || tags.Count > 0;
        CurrentSetEmptyText.IsVisible = !hasSelection;
        AddFilterGroupButton.IsEnabled = hasSelection;
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

        var included = _filterGroups.Where(condition => !condition.Negate).ToList();
        var excluded = _filterGroups.Where(condition => condition.Negate).ToList();

        if (included.Count > 0)
            FilterGroupsPanel.Children.Add(CreateConditionSection("Include", included, exclude: false));
        if (excluded.Count > 0)
            FilterGroupsPanel.Children.Add(CreateConditionSection("Exclude", excluded, exclude: true));
    }

    private Control CreateConditionSection(
        string title,
        IReadOnlyList<FilterGroupControls> conditions,
        bool exclude)
    {
        var rows = new StackPanel { Spacing = 6 };
        foreach (var condition in conditions)
            rows.Children.Add(CreateConditionSetRow(condition, exclude));

        var accent = exclude
            ? ThemeResources.Brush("Theme.Brush.DangerText")
            : Brush("#78A9E6");

        return new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 11.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = accent,
                    Margin = new Thickness(2, 0, 0, 0)
                },
                rows
            }
        };
    }

    private Control CreateConditionSetRow(FilterGroupControls condition, bool exclude)
    {
        var names = condition.GenreCtrl.SelectedItems
            .Select(DisplayGenreFilterName)
            .Concat(condition.TagCtrl.SelectedItems.Select(DisplayTagFilterName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var values = new TextBlock
        {
            Text = FormatConditionValues(names),
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };

        var removeBtn = new Button
        {
            Content = "×",
            Width = 24,
            Height = 24,
            Padding = new Thickness(0),
            FontSize = 15,
            Opacity = 0.62,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(removeBtn, "Remove this condition set");
        removeBtn.Click += (_, _) => RemoveFilterGroup(condition);

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 7
        };
        row.Children.Add(new Border
        {
            Width = 5,
            Height = 5,
            CornerRadius = new CornerRadius(3),
            Background = exclude
                ? ThemeResources.Brush("Theme.Brush.DangerText")
                : Brush("#78A9E6"),
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(values, 1);
        row.Children.Add(values);
        Grid.SetColumn(removeBtn, 2);
        row.Children.Add(removeBtn);

        return new Border
        {
            Background = Brush("#12FFFFFF"),
            CornerRadius = new CornerRadius(5),
            Padding = new Thickness(9, 5, 4, 5),
            Child = row
        };
    }

    private static string FormatConditionValues(IReadOnlyList<string> values)
    {
        return values.Count switch
        {
            0 => string.Empty,
            1 => values[0],
            2 => $"{values[0]} and {values[1]}",
            _ => $"{string.Join(", ", values.Take(values.Count - 1))} and {values[^1]}"
        };
    }

    private static string DisplayGenreFilterName(string genreName)
    {
        var parts = genreName.Split('→', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : genreName;
    }

    private static string DisplayTagFilterName(string tagName)
        => tagName;

    private FilterSection CreateGenreFilterSection(MultiSelectFilterControl genreCtrl)
    {
        string? selectedGroupName = null;
        var searchText = string.Empty;
        var choicesPanel = new UniformGrid
        {
            Columns = 2,
            ColumnSpacing = 7,
            RowSpacing = 7,
            Margin = new Thickness(0, 0, 0, 10)
        };
        var groupListPanel = new StackPanel { Spacing = 0 };
        var searchBox = new TextBox
        {
            Watermark = "Search",
            Width = 180,
            Height = 24,
            MinHeight = 24,
            MaxHeight = 24,
            IsVisible = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0)
        };
        searchBox.Classes.Add("compact-search");

        var searchButton = new Button
        {
            Width = 28,
            Height = 28,
            Padding = new Thickness(6),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Opacity = 0.66,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = new PathIcon
            {
                Data = Geometry.Parse("M229.66,218.34l-50.07-50.06a88.11,88.11,0,1,0-11.31,11.31l50.06,50.07a8,8,0,0,0,11.32-11.32ZM40,112a72,72,0,1,1,72,72A72.08,72.08,0,0,1,40,112Z"),
                Width = 15,
                Height = 15,
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary")
            }
        };
        ToolTip.SetTip(searchButton, "Search subgenres");

        var subgenreScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(10, 6),
            Content = choicesPanel
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
                .ToList();

            foreach (var genre in choices)
            {
                var isSelected = selected.Contains(genre.Name);
                var button = CreateGenreFilterChoiceButton(SubgenreName(genre), isSelected);
                button.Click += (_, _) =>
                {
                    var next = genreCtrl.SelectedItems.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (!next.Add(genre.Name))
                        next.Remove(genre.Name);
                    genreCtrl.SetSelectedItems(next);
                    FillChoices();
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

        void FillGroups()
        {
            groupListPanel.Children.Clear();
            var groups = new[] { new GenreGroupChoice(null, "All") }
                .Concat(Values.Genres
                    .Select(GroupName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .Select(name => new GenreGroupChoice(name, name)));

            foreach (var group in groups)
            {
                var selected = string.Equals(
                    group.GroupName,
                    selectedGroupName,
                    StringComparison.OrdinalIgnoreCase);
                var text = new TextBlock
                {
                    Text = group.Label,
                    FontSize = 10.5,
                    FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = selected
                        ? Brush("#78A9E6")
                        : ThemeResources.Brush("Theme.Brush.TextSecondary"),
                    Opacity = selected ? 1 : 0.72,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var item = new Border
                {
                    Height = 25,
                    Background = Brushes.Transparent,
                    Padding = new Thickness(2, 0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Child = text
                };
                item.PointerPressed += (_, e) =>
                {
                    selectedGroupName = group.GroupName;
                    FillGroups();
                    FillChoices();
                    e.Handled = true;
                };
                groupListPanel.Children.Add(item);
            }

            subgenreScroll.MaxHeight = Math.Max(25, groupListPanel.Children.Count * 25);
        }

        searchButton.Click += (_, _) =>
        {
            searchBox.IsVisible = !searchBox.IsVisible;
            searchButton.Opacity = searchBox.IsVisible ? 1 : 0.66;
            if (searchBox.IsVisible)
                Dispatcher.UIThread.Post(() => searchBox.Focus());
            else
                searchBox.Text = string.Empty;
        };
        searchBox.TextChanged += (_, _) =>
        {
            searchText = searchBox.Text ?? string.Empty;
            FillChoices();
        };

        FillGroups();
        FillChoices();

        var titleRow = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        titleRow.Children.Add(new TextBlock
        {
            Text = "Genres",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(searchBox, 1);
        titleRow.Children.Add(searchBox);
        Grid.SetColumn(searchButton, 2);
        titleRow.Children.Add(searchButton);

        var browser = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("118,13,*")
        };
        browser.Children.Add(groupListPanel);
        var separator = new Border
        {
            Width = 1,
            Background = ThemeResources.Brush("Theme.Brush.Divider"),
            Margin = new Thickness(6, 1)
        };
        Grid.SetColumn(separator, 1);
        browser.Children.Add(separator);
        Grid.SetColumn(subgenreScroll, 2);
        browser.Children.Add(subgenreScroll);

        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 10),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    titleRow,
                    browser
                }
            }
        };
        return new FilterSection(border, () =>
        {
            FillGroups();
            FillChoices();
        });
    }

    private FilterSection CreateTagFilterSection(MultiSelectFilterControl tagCtrl)
    {
        var panel = new StackPanel { Spacing = 9 };

        var header = new Grid();
        header.Children.Add(new TextBlock
        {
            Text = "Tags",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(header);

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

            var tags = Values.Tags
                .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var rows = new StackPanel { Spacing = 7 };
            var rowCount = Math.Max(1, (int)Math.Ceiling(tags.Count / 4d));
            var tagIndex = 0;

            for (var rowIndex = 0; rowIndex < rowCount && tagIndex < tags.Count; rowIndex++)
            {
                var remainingTags = tags.Count - tagIndex;
                var remainingRows = rowCount - rowIndex;
                var tagsInRow = (int)Math.Ceiling(remainingTags / (double)remainingRows);
                var row = new Grid { ColumnSpacing = 7 };

                for (var column = 0; column < tagsInRow; column++)
                {
                    var tag = tags[tagIndex++];
                    row.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(Math.Max(6, tag.Name.Length + 4), GridUnitType.Star)
                    });
                    var selected = tagCtrl.SelectedItems.Contains(TagFilterName(tag));
                    var button = CreateTagFilterChoiceButton(tag.Name, selected);
                    button.Click += (_, _) => Toggle(tag);
                    Grid.SetColumn(button, column);
                    row.Children.Add(button);
                }

                rows.Children.Add(row);
            }

            panel.Children.Add(rows);
        }

        RebuildTags();

        var border = new Border
        {
            Background = Brushes.Transparent,
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 10),
            Child = panel
        };
        return new FilterSection(border, RebuildTags);
    }

    private static Button CreateGenreFilterChoiceButton(string title, bool isSelected)
    {
        var accent = ThemeResources.Brush("Theme.Brush.TextSecondary");
        var text = new TextBlock
        {
            Text = title,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = isSelected
                ? accent
                : ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };

        return new Button
        {
            Content = text,
            Height = 32,
            Padding = new Thickness(9, 4),
            CornerRadius = new CornerRadius(5),
            Background = isSelected ? Brush("#263E6591") : Brushes.Transparent,
            BorderBrush = isSelected ? Brush("#7A78A9E6") : Brush("#36FFFFFF"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private static Button CreateTagFilterChoiceButton(string title, bool isSelected)
    {
        var content = new Grid();
        content.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush(isSelected
                ? "Theme.Brush.TextStrong"
                : "Theme.Brush.TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        });
        if (isSelected)
        {
            var check = new TextBlock
            {
                Text = "✓",
                FontSize = 11,
                Foreground = ThemeResources.Brush("Theme.Brush.Accent"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            content.Children.Add(check);
        }

        return new Button
        {
            Content = content,
            Height = 34,
            Padding = new Thickness(9, 3),
            CornerRadius = new CornerRadius(5),
            Background = isSelected ? Brush("#263E6591") : Brushes.Transparent,
            BorderBrush = isSelected ? Brush("#7A78A9E6") : Brush("#36FFFFFF"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
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
        ApplyFilterDefinitionChange();
    }

    // ─── Dialogs ──────────────────────────────────────────────────────────────

    private void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        ImportOverlay.Open();
    }

    private void OnChannelsClicked(object? sender, RoutedEventArgs e)
    {
        ChannelOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
        ChannelOverlay.Open();
    }

    private void OnMusicVideoClicked(object? sender, RoutedEventArgs e)
    {
        MusicVideoOverlay.Margin = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
        MusicVideoOverlay.Open();
    }

    private void OnRefreshLibraryClicked(object? sender, RoutedEventArgs e)
    {
        RefreshTrackList();
        if (ImportOverlay.IsVisible)
            ImportOverlay.RefreshQueue();
    }

    private void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        UpdateSettingsLayout();
        SettingsOverlay.Open();
    }

    private void UpdateSettingsLayout()
    {
        var playerClearance = new Thickness(0, 0, 0, PlayerBar.Bounds.Height);
        SettingsOverlay.Margin = playerClearance;
        FilterDrawer.Margin = playerClearance;
    }

    private void MoveFilterDrawerToRootOverlay()
    {
        if (FilterDrawer.Parent is not Panel currentParent)
            return;

        currentParent.Children.Remove(FilterDrawer);
        RootSurface.Children.Add(FilterDrawer);
    }

    private void ApplyAppearanceSettings(AppearanceSettings settings, bool refreshTrackRows)
    {
        var updatedSettings = settings.Clone().Clamp();
        var trackAppearanceChanged = TrackAppearanceChanged(_appearanceSettings, updatedSettings);
        _appearanceSettings = updatedSettings;
        PlayerDarkeningOverlay.Background = new SolidColorBrush(Color.FromArgb(
            ToByte(_appearanceSettings.PlayerBackgroundDarkening / 100d * 255),
            0x24, 0x24, 0x24));
        SpectrumVisualizer.IsVisible = _appearanceSettings.SpectrumVisualizerEnabled;
        SpectrumVisualizer.Height = _appearanceSettings.SpectrumVisualizerHeight;
        SpectrumVisualizer.Opacity = 0.40;
        SpectrumVisualizer.Sensitivity = _appearanceSettings.SpectrumVisualizerSensitivity / 100d;
        SpectrumVisualizer.Smoothing = _appearanceSettings.SpectrumVisualizerSmoothing / 100d;

        if (trackAppearanceChanged)
            foreach (var item in _allItems)
                item.ApplyAppearance(_appearanceSettings);

        ApplyAudioAtmosphere();
        if (refreshTrackRows && trackAppearanceChanged && _allItems.Count > 0)
            RefreshVisibleItemsSource((FileList.SelectedItem as TrackDisplayItem)?.Track.Id);
    }

    private static bool TrackAppearanceChanged(AppearanceSettings current, AppearanceSettings updated) =>
        current.TrackArtworkStrength != updated.TrackArtworkStrength
        || current.TrackArtworkBlur != updated.TrackArtworkBlur
        || current.TrackColorWashStrength != updated.TrackColorWashStrength
        || current.TrackColorWashReach != updated.TrackColorWashReach
        || current.CoverHaloStrength != updated.CoverHaloStrength
        || current.CoverHaloBlur != updated.CoverHaloBlur;

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
            var archivePath = await MusicLibraryService.Current.ExportPortableLibraryAsync(folders[0].Path.LocalPath);
            StatusText.Text = $"Exported Android library: {Path.GetFileName(archivePath)}";
            StatusText.IsVisible = true;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Export failed: {ex.Message}";
            StatusText.IsVisible = true;
        }
    }

    private void OnPlayerBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(PlayerBar).Properties.IsLeftButtonPressed)
            return;

        for (var visual = e.Source as Visual;
             visual is not null && visual != PlayerBar;
             visual = visual.GetVisualParent())
        {
            if (visual is Button or Slider)
                return;
        }

        if (EditTrackOverlay.IsOpen)
        {
            EditTrackOverlay.RequestClose();
            e.Handled = true;
            return;
        }

        if (_engine.ActiveTrackId < 0)
            return;

        var track = _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track;
        if (track is null)
            return;

        OpenTrackEditor(track);
        e.Handled = true;
    }

    private void OpenTrackEditor(MusicTrack track)
    {
        UpdateEditorBounds();
        EditTrackOverlay.Open(
            track,
            backdropFocus: _trackBackdropFocus.GetValueOrDefault(track.Id, 0.5));
    }

    private void PrepareActiveTrackEditor()
    {
        if (_engine.ActiveTrackId < 0)
            return;

        var track = _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)?.Track;
        if (track is not null)
            PrepareTrackEditor(track);
    }

    private void PrepareTrackEditor(MusicTrack track, bool force = false)
    {
        if (EditTrackOverlay.IsOpen || (!force && EditTrackOverlay.IsPreparedFor(track.Id)))
            return;

        EditTrackOverlay.InvalidatePreparedTrack();
        var currentTrack = _allItems.FirstOrDefault(item => item.Track.Id == track.Id)?.Track ?? track;
        EditTrackOverlay.Prepare(
            currentTrack,
            _trackBackdropFocus.GetValueOrDefault(track.Id, 0.5));
    }

    private async Task<bool> DeleteTrackFromEditorAsync(MusicTrack track)
    {
        if (_isDeletingTrack)
            return false;

        _isDeletingTrack = true;
        var deleted = false;
        try
        {
            if (_engine.ActiveTrackId == track.Id || _previewTrackId == track.Id)
            {
                FinishListeningSession(markSkipped: false);
                _previewPlaybackSnapshot = null;
                _isTrackPreviewActive = false;
                _previewTrackId = -1;
                ChannelOverlay.ClearActivePreview();
                _engine.Stop();
                NowPlayingText.Text = string.Empty;
                PlaybackInfoPanel.IsVisible = false;
                _nextTrackIndex = -1;
                ClearPlayerArtworkBackground();
            }

            // Let Stop()/pointer routing finish before the backing file, database
            // record and bound row are removed.
            await Task.Yield();
            var error = await MusicLibraryService.Current.DeleteTrackAsync(track);
            if (error is not null)
            {
                ShowToast(error);
                return false;
            }

            deleted = true;
            _trackBackdropFocus.Remove(track.Id);
            _pendingBackdropFocusTrackIds.Remove(track.Id);
            try
            {
                RemoveTrackFromCurrentLists(track.Id);
            }
            catch (Exception presentationException)
            {
                // The track is already deleted. Recover the presentation from the
                // database instead of letting a stale hover/selection crash the app.
                try
                {
                    RefreshTrackList();
                }
                catch
                {
                    // The next regular refresh will repair the view.
                }
                ShowToast($"Track deleted; view refresh recovered from: {presentationException.Message}");
                return true;
            }

            PersistPlayerSession();
            ShowToast("Track deleted");
            return true;
        }
        catch (Exception exception)
        {
            ShowToast(deleted
                ? $"Track deleted, but cleanup failed: {exception.Message}"
                : $"Could not delete track: {exception.Message}");
            return deleted;
        }
        finally
        {
            _isDeletingTrack = false;
        }
    }

    private void RemoveTrackFromCurrentLists(int trackId)
    {
        var selectedId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id;
        var deletedIndex = _filteredItems.FindIndex(item => item.Track.Id == trackId);

        _allItems.RemoveAll(item => item.Track.Id == trackId);
        _filteredItems.RemoveAll(item => item.Track.Id == trackId);
        _visibleItems.RemoveAll(item => item.Track.Id == trackId);
        _allTrackStyleIds.Remove(trackId);
        _allTrackGenreIds.Remove(trackId);
        _allTrackTagIds.Remove(trackId);

        if (selectedId == trackId)
            FileList.SelectedIndex = -1;

        RefreshVisibleItemsSource(selectedId == trackId ? null : selectedId);

        if (_filteredItems.Count == 0)
        {
            SetFilteredSelectedIndex(-1);
        }
        else if (selectedId == trackId)
        {
            SetFilteredSelectedIndex(Math.Clamp(deletedIndex, 0, _filteredItems.Count - 1));
        }

        UpdatePlaylistSummary();
        RefreshNextTrackPreview();
        UpdateFilterCounts();
        UpdateReviewFilterButton();
        RestartVisibleThumbnailLoad();
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
            case MediaShortcut.Stop:
                FinishListeningSession(markSkipped: false);
                _engine.Stop();
                break;
        }
    }

    private void OnSystemSeekRequested(TimeSpan offset)
    {
        if (_engine.State == EngineState.Stopped || _engine.TotalTime <= TimeSpan.Zero)
            return;
        var position = _engine.CurrentTime + offset;
        OnSystemPositionRequested(position);
    }

    private void OnSystemPositionRequested(TimeSpan position)
    {
        if (_engine.State == EngineState.Stopped || _engine.TotalTime <= TimeSpan.Zero)
            return;

        var seconds = Math.Clamp(position.TotalSeconds, 0, _engine.TotalTime.TotalSeconds);
        _engine.Seek(seconds / _engine.TotalTime.TotalSeconds);
        UpdatePlaybackPositionUi();
        _windowsMediaSession.NotifySeeked(_engine.CurrentTime);
    }

    private void OnSystemVolumeRequested(double volume)
    {
        VolumeSlider.Value = Math.Clamp(volume, 0, 1) * 100;
    }

    private void OnSystemLoopStatusRequested(string status)
    {
        if (status is not ("None" or "Track" or "Playlist")) return;
        _loopStatus = status;
        _windowsMediaSession.UpdateLoopStatus(status);
        RefreshNextTrackPreview();
    }

    private void OnSystemOpenUriRequested(Uri uri)
    {
        if (!uri.IsFile) return;

        string requestedPath;
        try { requestedPath = Path.GetFullPath(uri.LocalPath); }
        catch { return; }

        var index = _filteredItems.FindIndex(item =>
        {
            try
            {
                var trackPath = Path.GetFullPath(Path.Combine(Values.TracksDirectory, item.Track.FileName));
                return string.Equals(trackPath, requestedPath, StringComparison.Ordinal);
            }
            catch { return false; }
        });
        if (index >= 0) PlayTrackAt(index, isCrossfade: false);
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

    private void OnShuffleToggleClicked(object? sender, RoutedEventArgs e) => SetShuffle(!_shuffle);

    private void SetShuffle(bool shuffle)
    {
        if (_shuffle == shuffle) return;
        _shuffle = shuffle;
        _shufflePriorities.Clear();
        ApplyFilter();
        SetFilteredSelectedIndex(_filteredItems.Count > 0 ? 0 : -1);
        if (_filteredItems.Count > 0)
            FileList.ScrollIntoView(_filteredItems[0]);
        _restartQueueFromTopAfterCurrent = _engine.ActiveTrackId >= 0;
        _nextTrackIndex = GetQueueRestartIndex();
        UpdateUpcomingBar();
        UpdateShuffleButton();
        PersistPlayerSession();
    }

    private void UpdateShuffleButton()
    {
        ShuffleBtn.Opacity = _shuffle ? 1.0 : 0.35;
        ToolTip.SetTip(ShuffleBtn, _shuffle ? "Shuffle: On" : "Shuffle: Off");
        _windowsMediaSession.UpdateShuffle(_shuffle);
    }

    private void StartPlayback()
    {
        var idx = GetSelectedFilteredIndex();
        if (idx < 0 || idx >= _filteredItems.Count) return;

        PlayTrackAt(idx, isCrossfade: false);
    }

    private void PlayTrackAt(int filteredIndex, bool isCrossfade)
    {
        if (filteredIndex < 0 || filteredIndex >= _filteredItems.Count) return;

        var track = _filteredItems[filteredIndex].Track;
        var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
        _restartQueueFromTopAfterCurrent = false;

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
            _engine.Play(filePath, track.Id, fadeOut, fadeIn, LoudnessGainForTrack(track.Id));
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Playback failed: {ex.Message}";
            StatusText.IsVisible = true;
            return;
        }

        BeginListeningSession(track.Id);

        EnsureVisibleWindowAround(filteredIndex);
        SetFilteredSelectedIndex(filteredIndex);

        NowPlayingText.Text = track.Title;
        UpdateDiscordPresence();
        UpdatePlayerArtworkBackground(track);
        PlaybackInfoPanel.IsVisible = true;
        _nextTrackIndex = PeekNextTrackIndex(filteredIndex);
        UpdateUpcomingBar();
        UpdateButtonStates();
        RefreshPlayingMarkers();
        PersistPlayerSession();
        PrepareTrackEditor(track);
    }

    private static float LoudnessGainForTrack(int trackId)
    {
        var analysis = MusicLibraryService.Current.GetTrackAudioAnalysis(trackId);
        return PlaybackEngine.CalculateLoudnessGain(analysis?.IntegratedLoudness, analysis?.LoudnessRange);
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
            _engine.Play(filePath, track.Id, 0, 0, LoudnessGainForTrack(track.Id));
        }
        catch (Exception exception)
        {
            ShowToast($"Preview could not start: {exception.Message}");
            return;
        }

        _previewTrackId = track.Id;
        _isSeeking = false;
        var index = _filteredItems.FindIndex(item => item.Track.Id == track.Id);
        if (index >= 0)
        {
            EnsureVisibleWindowAround(index);
            SetFilteredSelectedIndex(index);
        }
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
        ChannelOverlay.ClearActivePreview();
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
            if (index >= 0)
            {
                EnsureVisibleWindowAround(index);
                SetFilteredSelectedIndex(index);
            }
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
        UpdateSystemMediaMetadata();
        UpdateButtonStates();
        UpdateDiscordPresence();
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
            _discordPresence.Clear();
            RefreshPlayingMarkers();
            UpdateUpcomingBar();
        }
    }

    private void OnTrackNaturallyEnded()
    {
        if (_isTrackPreviewActive)
        {
            ChannelOverlay.ClearActivePreview();
            _nextTrackIndex = -1;
            UpdateUpcomingBar();
            return;
        }
        FinishListeningSession(markSkipped: false);
        if (_loopStatus == "Track")
        {
            var currentIndex = GetCurrentPlayIndex();
            if (currentIndex >= 0)
            {
                PlayTrackAt(currentIndex, isCrossfade: false);
                return;
            }
        }
        NavigateNext(isManual: false);
    }

    private void OnProgressUpdated()
    {
        _windowsMediaSession.UpdatePosition(_engine.CurrentTime, _engine.TotalTime);
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
        if (_loopStatus == "Track") return -1;
        int nextIdx = currentFilteredIndex + 1;
        return nextIdx < _filteredItems.Count
            ? nextIdx
            : _loopStatus == "Playlist" ? 0 : -1;
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

        PlaylistSummaryText.Text = $"{_filteredItems.Count} tracks · {FormatPlaylistDuration(totalSeconds)}";
    }

    private void NavigateNext(bool isManual)
    {
        if (_isTrackPreviewActive) return;
        if (_filteredItems.Count == 0) { FullStop(); return; }

        var currentIdx = GetCurrentPlayIndex();

        int nextLinearIdx;
        if (_restartQueueFromTopAfterCurrent)
        {
            _restartQueueFromTopAfterCurrent = false;
            nextLinearIdx = GetQueueRestartIndex();
            if (nextLinearIdx < 0) { FullStop(); return; }
        }
        else if (currentIdx >= 0)
        {
            nextLinearIdx = currentIdx + 1;
            if (nextLinearIdx >= _filteredItems.Count)
            {
                if (_loopStatus == "Playlist") nextLinearIdx = 0;
                else { FullStop(); return; }
            }
        }
        else if (_engine.ActiveTrackId < 0)
        {
            var selIdx = GetSelectedFilteredIndex();
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

    private int GetQueueRestartIndex()
    {
        if (_filteredItems.Count == 0)
            return -1;

        return _engine.ActiveTrackId >= 0
               && _filteredItems[0].Track.Id == _engine.ActiveTrackId
               && _filteredItems.Count > 1
            ? 1
            : 0;
    }

    private void NavigatePrevious()
    {
        if (_isTrackPreviewActive) return;
        if (_filteredItems.Count == 0) return;

        var currentIdx = GetCurrentPlayIndex();
        int prevIdx;
        if (currentIdx < 0)
        {
            var selIdx = GetSelectedFilteredIndex();
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
        if (_playerArtworkTrackId == track.Id)
        {
            ApplyBackdropFocus(track.Id);
            SetPlayerArtworkBackground(_playerArtwork);
            SettingsOverlay.UpdateAppearancePreviewArtwork(
                _playerArtwork, _targetAmbientPrimary, _targetAmbientSecondary, track.Title);
            ApplyAudioAtmosphere();
            return;
        }

        PrepareOutgoingArtwork();
        var loadedArtwork = LoadPlayerArtwork(track);
        var artwork = loadedArtwork.Artwork;
        _playerArtwork = loadedArtwork.Artwork;
        _playerArtworkTrackId = track.Id;
        ApplyBackdropFocus(track.Id);
        BeginArtworkTransition(_previousPlayerArtwork is not null);
        SetAmbientPalette(loadedArtwork.Palette, artwork is not null);
        SetPlayerArtworkBackground(artwork);
        SettingsOverlay.UpdateAppearancePreviewArtwork(
            artwork, loadedArtwork.Palette.Primary, loadedArtwork.Palette.Secondary, track.Title);
        ApplyAudioAtmosphere();
    }

    private void PrepareOutgoingArtwork()
    {
        _outgoingAppScale = GetScale(AppArtworkBackground, 1.08);
        _outgoingPlayerScale = GetScale(PlayerArtworkBackground, 1.10);
        _outgoingAppBlur = GetBlur(AppArtworkBackground, 20);
        _outgoingPlayerBlur = GetBlur(PlayerArtworkBackground, 30);

        PlayerArtworkPreviousBackground.Source = null;
        AppArtworkPreviousBackground.Source = null;
        _previousPlayerArtwork?.Dispose();

        // The active artwork always becomes the sole faded layer. This is
        // intentionally independent of how far the previous transition got:
        // fast track changes must never bring an older faded cover back.
        _previousPlayerArtwork = _playerArtwork;
        _playerArtwork = null;

        PlayerArtworkPreviousBackground.Source = _previousPlayerArtwork;
        PlayerArtworkPreviousBackground.IsVisible = _previousPlayerArtwork is not null;
        PlayerArtworkPreviousBackground.Opacity = PlayerArtworkBackground.Opacity;
        AppArtworkPreviousBackground.Source = _previousPlayerArtwork;
        AppArtworkPreviousBackground.FocusX = AppArtworkBackground.FocusX;
        AppArtworkPreviousBackground.IsVisible = _previousPlayerArtwork is not null;
        AppArtworkPreviousBackground.Opacity = AppArtworkBackground.Opacity;

        // Hide and clear active before assigning the next bitmap. Otherwise
        // Avalonia can render the new source once with the previous opacity.
        PlayerArtworkBackground.Opacity = 0;
        AppArtworkBackground.Opacity = 0;
        PlayerArtworkBackground.Source = null;
        PlayerArtworkBackground.IsVisible = false;
        AppArtworkBackground.Source = null;
        AppArtworkBackground.IsVisible = false;
    }

    private void BeginArtworkTransition(bool hasOutgoingArtwork)
    {
        _artworkTransitionProgress = hasOutgoingArtwork ? 0 : 1;
        _artworkTransitionStartedAt = DateTimeOffset.UtcNow;
        StartAudioAtmosphereTimer();
    }

    private void SetPlayerArtworkBackground(Bitmap? artwork)
    {
        PlayerArtworkBackground.Opacity = 0;
        AppArtworkBackground.Opacity = 0;
        PlayerArtworkBackground.Source = artwork;
        PlayerArtworkBackground.IsVisible = artwork is not null;
        AppArtworkBackground.Source = artwork;
        AppArtworkBackground.IsVisible = artwork is not null;
        PlayerArtworkPreviousBackground.Source = _previousPlayerArtwork;
        PlayerArtworkPreviousBackground.IsVisible = _previousPlayerArtwork is not null;
        AppArtworkPreviousBackground.Source = _previousPlayerArtwork;
        AppArtworkPreviousBackground.IsVisible = _previousPlayerArtwork is not null;
    }

    private void ApplyBackdropFocus(int trackId) =>
        AppArtworkBackground.FocusX = _trackBackdropFocus.GetValueOrDefault(trackId, 0.5);

    private void OnEditorBackdropFocusChanged(int trackId, double focusX)
    {
        focusX = Math.Clamp(focusX, 0d, 1d);
        _trackBackdropFocus[trackId] = focusX;
        _pendingBackdropFocusTrackIds.Add(trackId);
        if (_playerArtworkTrackId == trackId)
            AppArtworkBackground.FocusX = focusX;
        _backdropFocusSaveTimer.Stop();
        _backdropFocusSaveTimer.Start();
    }

    private void FlushBackdropFocusSave()
    {
        _backdropFocusSaveTimer.Stop();
        foreach (var trackId in _pendingBackdropFocusTrackIds)
            if (_trackBackdropFocus.TryGetValue(trackId, out var focusX))
                AppSettingsStore.SaveTrackBackdropFocus(trackId, focusX);
        _pendingBackdropFocusTrackIds.Clear();
    }

    private static LoadedPlayerArtwork LoadPlayerArtwork(MusicTrack track)
    {
        try
        {
            var filePath = Path.Combine(Values.TracksDirectory, track.FileName);
            var artwork = ThumbnailService.ReadEmbeddedPlayerArtwork(filePath);
            artwork ??= track.Thumbnail is { Length: > 0 } thumbnail
                ? thumbnail
                : MusicLibraryService.Current.GetTrackThumbnail(track.Id);
            if (artwork is not { Length: > 0 })
                return new LoadedPlayerArtwork(null, DefaultAmbientPalette);

            using var stream = new MemoryStream(artwork);
            return new LoadedPlayerArtwork(
                new Bitmap(stream),
                ExtractAmbientPalette(artwork));
        }
        catch
        {
            return new LoadedPlayerArtwork(null, DefaultAmbientPalette);
        }
    }

    private void ClearPlayerArtworkBackground(bool disposeCache = false)
    {
        SettingsOverlay.UpdateAppearancePreviewArtwork(
            null, DefaultAmbientPalette.Primary, DefaultAmbientPalette.Secondary, "Preview track");
        PlayerArtworkBackground.Source = null;
        PlayerArtworkBackground.IsVisible = false;
        AppArtworkBackground.Source = null;
        AppArtworkBackground.IsVisible = false;
        PlayerArtworkPreviousBackground.Source = null;
        PlayerArtworkPreviousBackground.IsVisible = false;
        AppArtworkPreviousBackground.Source = null;
        AppArtworkPreviousBackground.IsVisible = false;
        _artworkTransitionProgress = 1;
        SetAmbientPalette(DefaultAmbientPalette, hasArtwork: false);
        ResetAudioAtmosphere();

        _playerArtwork?.Dispose();
        _previousPlayerArtwork?.Dispose();
        _playerArtwork = null;
        _previousPlayerArtwork = null;
        _playerArtworkTrackId = -1;
    }

    private void SetAmbientPalette(AmbientPalette palette, bool hasArtwork)
    {
        _ambientStartPrimary = _ambientPrimary;
        _ambientStartSecondary = _ambientSecondary;
        _targetAmbientPrimary = palette.Primary;
        _targetAmbientSecondary = palette.Secondary;
        _hasArtworkPalette = hasArtwork;
        StartAudioAtmosphereTimer();
    }

    private static AmbientPalette ExtractAmbientPalette(byte[] artwork)
    {
        using var bitmap = SKBitmap.Decode(artwork);
        if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0)
            return DefaultAmbientPalette;

        const int hueBinCount = 18;
        var bins = new AmbientColorBin[hueBinCount];
        for (var index = 0; index < bins.Length; index++)
            bins[index] = new AmbientColorBin();

        var step = Math.Max(1, (int)Math.Sqrt((bitmap.Width * bitmap.Height) / 5000d));
        var opaqueSampleCount = 0;
        var chromaticSampleCount = 0;
        double sampledLightness = 0;
        for (var y = step / 2; y < bitmap.Height; y += step)
        {
            for (var x = step / 2; x < bitmap.Width; x += step)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Alpha < 160)
                    continue;

                pixel.ToHsl(out var hue, out var saturation, out var lightness);
                opaqueSampleCount++;
                sampledLightness += lightness;
                if (saturation < 18 || lightness < 7 || lightness > 92)
                    continue;

                chromaticSampleCount++;
                var midtoneWeight = 1 - Math.Abs((lightness / 100d) - 0.5) * 0.55;
                var weight = (0.25 + saturation / 100d) * midtoneWeight;
                var binIndex = Math.Clamp((int)(hue / (360d / hueBinCount)), 0, hueBinCount - 1);
                bins[binIndex].Add(pixel, weight);
            }
        }

        // A small warm accent must not define an otherwise white, black or
        // grayscale cover. In that case the artwork's overall brightness is
        // more representative than the strongest remaining hue.
        var chromaticCoverage = opaqueSampleCount > 0
            ? chromaticSampleCount / (double)opaqueSampleCount
            : 0;
        if (chromaticCoverage < 0.18)
            return CreateNeutralAmbientPalette(opaqueSampleCount > 0
                ? sampledLightness / opaqueSampleCount
                : 50);

        var primaryIndex = Array.FindIndex(bins, bin => ReferenceEquals(bin, bins.MaxBy(candidate => candidate.Weight)));
        if (primaryIndex < 0 || bins[primaryIndex].Weight <= 0)
            return CreateNeutralAmbientPalette(sampledLightness / Math.Max(1, opaqueSampleCount));

        var secondaryCandidates = Enumerable.Range(0, bins.Length)
            .Where(index => bins[index].Weight > 0 && HueBinDistance(index, primaryIndex, hueBinCount) >= 2)
            .ToList();
        var secondaryIndex = secondaryCandidates.Count > 0
            ? secondaryCandidates.MaxBy(index => bins[index].Weight * (1 + HueBinDistance(index, primaryIndex, hueBinCount) * 0.08))
            : primaryIndex;

        var primary = NormalizeAmbientColor(bins[primaryIndex].AverageColor());
        var secondary = secondaryIndex != primaryIndex && bins[secondaryIndex].Weight > 0
            ? NormalizeAmbientColor(bins[secondaryIndex].AverageColor())
            : RotateAmbientColor(primary, 46);
        return new AmbientPalette(primary, secondary);
    }

    private static AmbientPalette CreateNeutralAmbientPalette(double artworkLightness)
    {
        // Bright neutral covers produce a light silver graph, while dark
        // covers remain visible through a softer graphite-to-gray gradient.
        var primaryLightness = Math.Clamp(54 + artworkLightness * 0.36, 58, 88);
        var secondaryLightness = Math.Clamp(primaryLightness - 25, 36, 66);
        var primary = SKColor.FromHsl(215, 5, (float)primaryLightness);
        var secondary = SKColor.FromHsl(215, 8, (float)secondaryLightness);
        return new AmbientPalette(
            Color.FromRgb(primary.Red, primary.Green, primary.Blue),
            Color.FromRgb(secondary.Red, secondary.Green, secondary.Blue));
    }

    private static int HueBinDistance(int left, int right, int count)
    {
        var distance = Math.Abs(left - right);
        return Math.Min(distance, count - distance);
    }

    private static Color NormalizeAmbientColor(SKColor color)
    {
        color.ToHsl(out var hue, out var saturation, out var lightness);
        var normalized = SKColor.FromHsl(
            hue,
            Math.Clamp(saturation, 55, 82),
            Math.Clamp(lightness, 40, 56));
        return Color.FromRgb(normalized.Red, normalized.Green, normalized.Blue);
    }

    private static Color RotateAmbientColor(Color color, float degrees)
    {
        var source = new SKColor(color.R, color.G, color.B);
        source.ToHsl(out var hue, out var saturation, out var lightness);
        var rotated = SKColor.FromHsl((hue + degrees) % 360, saturation, lightness);
        return Color.FromRgb(rotated.Red, rotated.Green, rotated.Blue);
    }

    private sealed record LoadedPlayerArtwork(Bitmap? Artwork, AmbientPalette Palette);
    private sealed record LoadedTrackThumbnail(byte[] Artwork, AmbientPalette Palette);
    private sealed record AmbientPalette(Color Primary, Color Secondary);

    private sealed class AmbientColorBin
    {
        private double _red;
        private double _green;
        private double _blue;

        public double Weight { get; private set; }

        public void Add(SKColor color, double weight)
        {
            _red += color.Red * weight;
            _green += color.Green * weight;
            _blue += color.Blue * weight;
            Weight += weight;
        }

        public SKColor AverageColor() => Weight <= 0
            ? SKColors.Transparent
            : new SKColor(
                ToByte(_red / Weight),
                ToByte(_green / Weight),
                ToByte(_blue / Weight));
    }

    // ─── Audio-reactive atmosphere ───────────────────────────────────────────

    private void OnAudioLevelUpdated(PlaybackAudioLevel level)
    {
        if (_engine.State != EngineState.Playing)
            return;

        _targetEnergy = level.Energy;
        _targetBass = level.Bass;
        _targetTreble = level.Treble;
        SpectrumVisualizer.SetSpectrum(level.Spectrum);
        SettingsOverlay.UpdateAppearancePreviewSpectrum(level.Spectrum);
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
        SpectrumVisualizer.SetSpectrum(null);
        SettingsOverlay.UpdateAppearancePreviewSpectrum(null);
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
        SpectrumVisualizer.SetSpectrum(null);
        SettingsOverlay.UpdateAppearancePreviewSpectrum(null);
        SettingsOverlay.UpdateAppearancePreviewAudio(0, 0, 0);
        ApplyAudioAtmosphere();
    }

    private void UpdateAudioReactiveAtmosphere()
    {
        UpdateArtworkTransition();
        var configuredEasing = 0.04 + _appearanceSettings.AudioResponseSpeed / 100d * 0.24;
        var easing = _engine.State == EngineState.Playing ? configuredEasing : 0.10;
        _visualEnergy = Approach(_visualEnergy, _targetEnergy, easing);
        _visualBass = Approach(_visualBass, _targetBass, easing);
        _visualTreble = Approach(_visualTreble, _targetTreble, easing);
        SpectrumVisualizer.Advance();
        SettingsOverlay.UpdateAppearancePreviewAudio(_visualEnergy, _visualBass, _visualTreble);

        ApplyAudioAtmosphere();

        if (_engine.State == EngineState.Playing)
            return;

        if (_visualEnergy < 0.003
            && _visualBass < 0.003
            && _visualTreble < 0.003
            && SpectrumVisualizer.IsAtRest
            && !IsArtworkTransitionActive
            && AmbientPaletteSettled())
        {
            ResetAudioAtmosphere();
            _atmosphereTimer.Stop();
        }
    }

    private void ApplyAudioAtmosphere()
    {
        var reaction = _appearanceSettings.PlayerAudioReaction / 100d;
        var energy = SoftLimit(_visualEnergy) * reaction;
        var bass = SoftLimit(_visualBass * _appearanceSettings.AudioBassSensitivity / 100d) * reaction;
        var treble = SoftLimit(_visualTreble * _appearanceSettings.AudioTrebleSensitivity / 100d) * reaction;
        var motionReaction = _appearanceSettings.AudioArtworkMotion / 100d;
        var blurReaction = _appearanceSettings.AudioBlurReaction / 100d;
        var colorReaction = _appearanceSettings.AudioColorReaction / 100d;
        // Lift quiet passages (and low app-volume levels) without making the
        // high end grow linearly into an overpowering background.
        var visibilityEnergy = Math.Sqrt(energy);
        var hasArtwork = AppArtworkBackground.IsVisible
                         || PlayerArtworkBackground.IsVisible
                         || AppArtworkPreviousBackground.IsVisible
                         || PlayerArtworkPreviousBackground.IsVisible;
        var transition = IsArtworkTransitionActive
            ? SmoothStep(_artworkTransitionProgress)
            : 1;
        _ambientPrimary = MixColor(_ambientStartPrimary, _targetAmbientPrimary, transition);
        _ambientSecondary = MixColor(_ambientStartSecondary, _targetAmbientSecondary, transition);
        SpectrumVisualizer.SetColors(_ambientPrimary, _ambientSecondary);

        var incomingMix = IsArtworkTransitionActive ? Math.Sin(transition * Math.PI / 2) : 1;
        var outgoingMix = IsArtworkTransitionActive ? Math.Cos(transition * Math.PI / 2) : 0;
        var appOpacity = Math.Clamp(
            _appearanceSettings.LibraryBackdropStrength / 100d + visibilityEnergy * 0.21, 0, 1);
        var playerOpacity = Math.Clamp(
            _appearanceSettings.PlayerArtworkStrength / 100d + visibilityEnergy * 0.15, 0, 1);
        AppArtworkBackground.Opacity = AppArtworkBackground.IsVisible ? appOpacity * incomingMix : 0;
        PlayerArtworkBackground.Opacity = PlayerArtworkBackground.IsVisible ? playerOpacity * incomingMix : 0;
        AppArtworkPreviousBackground.Opacity = AppArtworkPreviousBackground.IsVisible ? appOpacity * outgoingMix : 0;
        PlayerArtworkPreviousBackground.Opacity = PlayerArtworkPreviousBackground.IsVisible ? playerOpacity * outgoingMix : 0;

        SetScale(AppArtworkBackground, 1.12 - transition * 0.04 + bass * 0.048 * transition * motionReaction);
        SetScale(PlayerArtworkBackground, 1.14 - transition * 0.04 + bass * 0.035 * transition * motionReaction);
        SetScale(AppArtworkPreviousBackground, Approach(_outgoingAppScale, 1.055, transition));
        SetScale(PlayerArtworkPreviousBackground, Approach(_outgoingPlayerScale, 1.075, transition));
        SetBlur(AppArtworkBackground,
            _appearanceSettings.LibraryBackdropBlur + (1 - transition) * 8 + energy * 8.0 * blurReaction);
        SetBlur(PlayerArtworkBackground,
            _appearanceSettings.PlayerArtworkBlur + (1 - transition) * 10
            + (energy * 6.0 + treble * 4.0) * blurReaction);
        SetBlur(AppArtworkPreviousBackground, Approach(
            _outgoingAppBlur, _appearanceSettings.LibraryBackdropBlur + 14, transition));
        SetBlur(PlayerArtworkPreviousBackground, Approach(
            _outgoingPlayerBlur, _appearanceSettings.PlayerArtworkBlur + 16, transition));

        var primary = MixColor(_ambientPrimary, Colors.White,
            (energy * 0.08 + treble * 0.05) * colorReaction);
        var secondary = MixColor(_ambientSecondary, Colors.White, energy * 0.05 * colorReaction);
        var artworkLift = hasArtwork && _hasArtworkPalette ? 1d : 0d;

        var atmosphere = _appearanceSettings.PlayerColorAtmosphere / 100d;
        _appAmbientPrimaryStop.Color = WithAlpha(primary,
            (28 + artworkLift * 20 + (energy * 32 + treble * 12) * colorReaction) * atmosphere);
        _appAmbientSecondaryStop.Color = WithAlpha(secondary,
            (16 + artworkLift * 16 + energy * 20 * colorReaction) * atmosphere);
        var overlayPrimary = WithAlpha(primary, 220);
        var overlaySecondary = WithAlpha(secondary, 150);
        _filterAmbientPrimaryStop.Color = overlayPrimary;
        _filterAmbientSecondaryStop.Color = overlaySecondary;
        EditTrackOverlay.SetAtmosphereColors(overlayPrimary, overlaySecondary);
        ImportOverlay.SetAtmosphereColors(overlayPrimary, overlaySecondary);
        ChannelOverlay.SetAtmosphereColors(overlayPrimary, overlaySecondary);
        _playerAmbientPrimaryStop.Color = WithAlpha(primary,
            (34 + artworkLift * 24 + (energy * 34 + bass * 10) * colorReaction) * atmosphere);
        _playerAmbientSecondaryStop.Color = WithAlpha(secondary,
            (24 + artworkLift * 20 + (energy * 24 + treble * 8) * colorReaction) * atmosphere);
        _playerTopGlowBrush.Color = WithAlpha(primary,
            (24 + artworkLift * 12 + (energy * 60 + treble * 24) * colorReaction) * atmosphere);
        _playerChromeEdgeBrush.Color = WithAlpha(
            MixColor(primary, secondary, 0.35),
            (24 + artworkLift * 8 + energy * 30 * colorReaction) * atmosphere);
    }

    private static double Approach(double current, double target, double amount) =>
        current + (target - current) * amount;

    private bool IsArtworkTransitionActive =>
        _previousPlayerArtwork is not null && _artworkTransitionProgress < 1;

    private void UpdateArtworkTransition()
    {
        if (!IsArtworkTransitionActive)
            return;

        _artworkTransitionProgress = Math.Clamp(
            (DateTimeOffset.UtcNow - _artworkTransitionStartedAt).TotalMilliseconds
            / ArtworkTransitionDuration.TotalMilliseconds,
            0,
            1);
        if (_artworkTransitionProgress < 1)
            return;

        PlayerArtworkPreviousBackground.Source = null;
        PlayerArtworkPreviousBackground.IsVisible = false;
        AppArtworkPreviousBackground.Source = null;
        AppArtworkPreviousBackground.IsVisible = false;
        _previousPlayerArtwork?.Dispose();
        _previousPlayerArtwork = null;
    }

    private static double SmoothStep(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        return progress * progress * (3 - 2 * progress);
    }

    private bool AmbientPaletteSettled() =>
        ColorDistance(_ambientPrimary, _targetAmbientPrimary) < 2
        && ColorDistance(_ambientSecondary, _targetAmbientSecondary) < 2;

    private static Color MixColor(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            ToByte(from.R + (to.R - from.R) * amount),
            ToByte(from.G + (to.G - from.G) * amount),
            ToByte(from.B + (to.B - from.B) * amount));
    }

    private static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb(ToByte(alpha), color.R, color.G, color.B);

    private static double ColorDistance(Color left, Color right) =>
        Math.Abs(left.R - right.R)
        + Math.Abs(left.G - right.G)
        + Math.Abs(left.B - right.B);

    private static double SoftLimit(double value) =>
        Math.Clamp(1 - Math.Exp(-Math.Max(0, value) * 1.45), 0, 1);

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);

    private static void SetScale(Control image, double scale)
    {
        if (image.RenderTransform is ScaleTransform transform)
        {
            transform.ScaleX = scale;
            transform.ScaleY = scale;
        }
    }

    private static double GetScale(Control image, double fallback) =>
        image.RenderTransform is ScaleTransform transform
            ? transform.ScaleX
            : fallback;

    private static void SetBlur(Control image, double radius)
    {
        if (image.Effect is BlurEffect blur)
            blur.Radius = radius;
    }

    private static double GetBlur(Control image, double fallback) =>
        image.Effect is BlurEffect blur
            ? blur.Radius
            : fallback;

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
        _windowsMediaSession.NotifySeeked(_engine.CurrentTime);
        UpdateDiscordPresence();
    }

    // ─── UI helpers ───────────────────────────────────────────────────────────

    private void UpdateDiscordPresence()
    {
        if (_engine.State == EngineState.Stopped)
        {
            _discordPresence.Clear();
            return;
        }

        var item = _filteredItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)
                   ?? _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId);
        if (item is null)
            return;

        _discordPresence.Update(item, _engine.State, _engine.CurrentTime, _engine.TotalTime);
    }

    private void UpdateSystemMediaMetadata()
    {
        if (_engine.State == EngineState.Stopped)
            return;

        var item = _filteredItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId)
                   ?? _allItems.FirstOrDefault(item => item.Track.Id == _engine.ActiveTrackId);
        if (item is null)
            return;

        var filePath = Path.Combine(Values.TracksDirectory, item.Track.FileName);
        var artworkUri = MprisArtworkCache.GetArtworkUri(item.Track, filePath);

        _windowsMediaSession.UpdateMetadata(
            item.Track.Id,
            item.Track.Title,
            item.ChannelText,
            _engine.CurrentTime,
            _engine.TotalTime,
            filePath,
            artworkUri,
            canGoNext: PeekNextTrackIndex(GetCurrentPlayIndex()) >= 0,
            canGoPrevious: GetCurrentPlayIndex() > 0);
    }

    private void UpdateButtonStates()
    {
        var isPlaying = _engine.State == EngineState.Playing;
        PlayIcon.IsVisible = !isPlaying;
        PauseIcon.IsVisible = isPlaying;
    }

    private void RefreshPlayingMarkers()
    {
        if (_filteredItems.Count == 0) return;

        var selectedId = (FileList.SelectedItem as TrackDisplayItem)?.Track.Id ?? -1;

        foreach (var item in _filteredItems)
            item.IsPlaying = item.Track.Id == _engine.ActiveTrackId;

        var targetId = selectedId >= 0 ? selectedId : _engine.ActiveTrackId;
        if (targetId >= 0)
        {
            var idx = _filteredItems.FindIndex(i => i.Track.Id == targetId);
            if (idx >= 0)
                EnsureVisibleWindowAround(idx);
        }

        RefreshVisibleItemsSource(selectedId >= 0 ? selectedId : null);
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
        var startsNewSession = _shufflePriorities.Count == 0;
        var missingTracks = _filteredItems
            .Where(item => !_shufflePriorities.ContainsKey(item.Track.Id))
            .Select(item => item.Track)
            .ToList();

        if (missingTracks.Count > 0)
        {
            var usageByTrackId = MusicLibraryService.Current.GetAllTrackUsageStats();
            var generatedPriorities = TrackShuffleService.CreatePriorities(
                _filteredItems.Select(item => item.Track).ToList(),
                usageByTrackId,
                Values.Ratings,
                _rng,
                DateTimeOffset.UtcNow);

            foreach (var track in missingTracks)
                _shufflePriorities[track.Id] = generatedPriorities[track.Id];
        }

        if (startsNewSession && _engine.ActiveTrackId >= 0
                             && _shufflePriorities.ContainsKey(_engine.ActiveTrackId))
            _shufflePriorities[_engine.ActiveTrackId] = double.NegativeInfinity;

        _filteredItems = _filteredItems
            .OrderBy(item => _shufflePriorities[item.Track.Id])
            .ToList();
    }

    private static string FormatDuration(int seconds)
    {
        var m = seconds / 60;
        var s = seconds % 60;
        return $"{m:D2}:{s:D2}";
    }
}
