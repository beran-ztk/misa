using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Resona.Core;
using SkiaSharp;
using System.IO.Compression;

namespace Resona.Companion.Views;

public partial class MainView : UserControl
{
    private readonly ICompanionAudioPlayer _audio = CompanionServices.AudioPlayer;
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _artworkTimer;
    private bool _systemBarsConfigured;
    private double _safeAreaTop;
    private double _safeAreaBottom;

    private LoadedMusicLibrary _loadedLibrary = new("", PortableMusicLibrary.Empty);

    private List<PortableTrack> _filteredTracks = [];
    private readonly List<FilterGroupControls> _filterGroups = [];
    private const int ThumbnailCacheCapacity = 192;
    private static readonly TimeSpan AutomaticArtworkTransitionDuration = TimeSpan.FromSeconds(6.5);
    private static readonly TimeSpan ManualArtworkTransitionDuration = TimeSpan.FromSeconds(1.8);
    private static readonly AmbientPalette DefaultAmbientPalette = new(
        Color.FromRgb(91, 110, 72),
        Color.FromRgb(74, 64, 105));

    private readonly Dictionary<string, Bitmap?> _thumbnailCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Queue<string> _thumbnailCacheOrder = new();
    private readonly Random _rng = new();
    private Bitmap? _activeArtwork;
    private Bitmap? _fadedArtwork;
    private string? _activeArtworkFileName;
    private DateTimeOffset _artworkTransitionStartedAt;
    private TimeSpan _artworkTransitionDuration = ManualArtworkTransitionDuration;
    private double _artworkTransitionProgress = 1;
    private AmbientPalette _activeAmbientPalette = DefaultAmbientPalette;
    private AmbientPalette _fadedAmbientPalette = DefaultAmbientPalette;
    private int _currentIndex = -1;
    private string? _currentTrackFileName;
    private bool _isSeeking;
    private bool _shuffle;
    private bool _showReviewOnly;
    private bool _updatingReviewFilterUi;
    private bool _updatingPresetUi;
    private DateTime _lastMediaUpdate = DateTime.MinValue;
    private CancellationTokenSource? _toastCts;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreFilter,
        MultiSelectFilterControl StyleFilter,
        MultiSelectFilterControl TagFilter,
        CheckBox NegateBox,
        StackPanel Container);

    private sealed class TrackRow
    {
        private static readonly IBrush TransparentBrush = Brushes.Transparent;
        private readonly MainView _owner;

        public TrackRow(MainView owner, PortableTrack track, bool isCurrent)
        {
            _owner = owner;
            Track = track;
            IsCurrent = isCurrent;
        }

        public PortableTrack Track { get; }
        public bool IsCurrent { get; }
        public bool IsMarkedForReview => Track.NeedsReview;
        public Bitmap? Cover => _owner.LoadThumbnail(Track);

        public string Title => Track.Title;
        public string MetadataText => string.Join(" · ", new[]
            {
                Track.ChannelName,
                Track.GenreText,
                Track.StyleText,
                string.Join("  ", (Track.Tags ?? []).Select(tag => $"# {tag}"))
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        public string DurationText => Track.DurationText;
        public string Rating => string.IsNullOrWhiteSpace(Track.Rating) ? "None" : Track.Rating;
        public IBrush RatingBackground => Rating switch
        {
            "Timeless" => new SolidColorBrush(Color.FromArgb(70, 132, 105, 36)),
            "Amazing" => new SolidColorBrush(Color.FromArgb(70, 120, 79, 42)),
            "Great" => new SolidColorBrush(Color.FromArgb(68, 35, 105, 58)),
            "Good" => new SolidColorBrush(Color.FromArgb(66, 31, 93, 82)),
            "Okay" => new SolidColorBrush(Color.FromArgb(62, 83, 84, 64)),
            "Avoid" => new SolidColorBrush(Color.FromArgb(66, 124, 47, 40)),
            _ => new SolidColorBrush(Color.FromArgb(60, 103, 76, 42))
        };
        public IBrush RatingBorder => Rating switch
        {
            "Timeless" => new SolidColorBrush(Color.FromArgb(185, 219, 184, 85)),
            "Amazing" => new SolidColorBrush(Color.FromArgb(180, 221, 144, 78)),
            "Great" => new SolidColorBrush(Color.FromArgb(175, 83, 176, 105)),
            "Good" => new SolidColorBrush(Color.FromArgb(170, 76, 164, 139)),
            "Okay" => new SolidColorBrush(Color.FromArgb(150, 139, 144, 108)),
            "Avoid" => new SolidColorBrush(Color.FromArgb(170, 201, 82, 68)),
            _ => new SolidColorBrush(Color.FromArgb(160, 190, 139, 69))
        };
        public IBrush RatingForeground => Rating switch
        {
            "Timeless" => new SolidColorBrush(Color.FromRgb(255, 230, 150)),
            "Amazing" => new SolidColorBrush(Color.FromRgb(247, 195, 132)),
            "Great" => new SolidColorBrush(Color.FromRgb(188, 242, 185)),
            "Good" => new SolidColorBrush(Color.FromRgb(176, 232, 212)),
            "Okay" => new SolidColorBrush(Color.FromRgb(226, 224, 194)),
            "Avoid" => new SolidColorBrush(Color.FromRgb(246, 175, 160)),
            _ => new SolidColorBrush(Color.FromRgb(243, 203, 128))
        };
        public IBrush CurrentBackground => IsCurrent
            ? CompanionTheme.Brush("Mobile.Brush.SurfaceSelected")
            : TransparentBrush;
        public IBrush CurrentAccent => IsCurrent
            ? CompanionTheme.Brush("Mobile.Brush.AccentStrong")
            : TransparentBrush;
    }

    public MainView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) => ConfigureSystemBars();
        if (Application.Current?.ApplicationLifetime is IActivatableLifetime activatableLifetime)
            activatableLifetime.Activated += (_, _) => Dispatcher.UIThread.Post(ConfigureSystemBars);

        SearchBox.TextChanged += (_, _) =>
        {
            SearchButton.Opacity = SearchBox.IsVisible || !string.IsNullOrWhiteSpace(SearchBox.Text) ? 1 : 0.72;
            ApplyFilter();
        };
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();

        ProgressSlider.AddHandler(PointerPressedEvent, OnProgressPressed, RoutingStrategies.Tunnel);
        ProgressSlider.AddHandler(PointerReleasedEvent, OnProgressReleased, RoutingStrategies.Tunnel);

        _audio.PlaybackEnded += () => Dispatcher.UIThread.Post(() => PlayNext(isAutomaticTransition: true));
        CompanionServices.MediaControls.CommandRequested += OnMediaCommandRequested;
        CompanionServices.MediaControls.SeekRequested += OnMediaSeekRequested;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
        _timer.Start();

        _artworkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _artworkTimer.Tick += (_, _) => UpdateArtworkTransition();

        _ = LoadLibraryAsync();
    }

    private void ConfigureSystemBars()
    {
        var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (insetsManager is null)
            return;

        insetsManager.DisplayEdgeToEdgePreference = true;
        insetsManager.SystemBarColor = Colors.Transparent;
        ApplySafeArea(insetsManager.SafeAreaPadding);

        if (!_systemBarsConfigured)
        {
            _systemBarsConfigured = true;
            insetsManager.SafeAreaChanged += (_, args) => ApplySafeArea(args.SafeAreaPadding);
        }
    }

    private void ApplySafeArea(Thickness safeArea)
    {
        // Android can briefly report zero insets while system-bar colors are
        // changing. Keep the last real values so content does not jump behind
        // the status or gesture bar during artwork and activity transitions.
        if (safeArea.Top > 0)
            _safeAreaTop = safeArea.Top;
        if (safeArea.Bottom > 0)
            _safeAreaBottom = safeArea.Bottom;

        HeaderBar.Padding = new Thickness(14, 6 + _safeAreaTop, 12, 6);
        PlayerContent.Margin = new Thickness(12, 5, 12, 6 + _safeAreaBottom);
    }

    private async Task LoadLibraryAsync()
    {
        try
        {
            ClearThumbnailCache();
            var cloud = new DeviceLibraryCloudClient();
            if (cloud.LoadConnection() is not null)
            {
                try
                {
                    using var refreshTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await cloud.RefreshMetadataAsync(refreshTimeout.Token);
                }
                catch (Exception ex)
                {
                    // A cached library remains usable while the server is unavailable.
                    SetStatus($"Cloud refresh failed: {ex.Message}");
                }
            }
            _loadedLibrary = await PortableLibraryStore.LoadAsync(CompanionServices.LibraryStorage.LibraryDirectory);
            SetStatus();
        }
        catch (Exception ex)
        {
            _loadedLibrary = new LoadedMusicLibrary(
                CompanionServices.LibraryStorage.LibraryDirectory,
                PortableMusicLibrary.Empty);
            SetStatus($"Could not load library: {ex.Message}");
        }

        PopulateFilters();
        ApplyFilter();
        UpdateReviewButton();
        UpdateReviewFilterButton();
    }

    private void PopulateFilters()
    {
        PopulatePresets();
        ApplyDefaultRatingFilter();
        RebuildFilterGroups();
    }

    private void ApplyDefaultRatingFilter()
    {
        var ratings = _loadedLibrary.Library.Ratings;
        RatingFilter.SetItems(ratings);
        RatingFilter.SetSelectedItems(
            ratings.Where(rating => !string.Equals(rating, "Avoid", StringComparison.OrdinalIgnoreCase)),
            notify: false);
    }

    private void PopulatePresets()
    {
        _updatingPresetUi = true;
        var names = (_loadedLibrary.Library.FilterPresets ?? [])
            .Select(preset => preset.Name)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        PresetBox.ItemsSource = names;
        PresetBox.SelectedIndex = -1;
        PresetBox.IsEnabled = names.Count > 0;
        _updatingPresetUi = false;
    }

    private void ApplyFilter()
    {
        _filteredTracks = PortableTrackFilter.Apply(
            _loadedLibrary.Library.Tracks,
            SearchBox.Text,
            RatingFilter.SelectedItems,
            _filterGroups
                .Select(group => new PortableFilterGroup(
                    group.GenreFilter.SelectedItems.ToList(),
                    group.StyleFilter.SelectedItems.ToList(),
                    group.TagFilter.SelectedItems.ToList(),
                    group.NegateBox.IsChecked == true))
                .ToList());

        if (_shuffle)
            ShuffleFilteredTracks();

        if (_showReviewOnly)
            _filteredTracks = _filteredTracks
                .Where(track => track.NeedsReview)
                .ToList();

        _currentIndex = string.IsNullOrWhiteSpace(_currentTrackFileName)
            ? -1
            : _filteredTracks.FindIndex(track => string.Equals(
                track.FileName,
                _currentTrackFileName,
                StringComparison.OrdinalIgnoreCase));

        RefreshTrackRows();
        UpdatePlaylistSummary();
        UpdateFilterCounts();
    }

    private void RefreshTrackRows(bool scrollToCurrent = false)
    {
        var rows = _filteredTracks
            .Select((track, index) => new TrackRow(this, track, index == _currentIndex))
            .ToList();

        TrackList.ItemsSource = rows;
        TrackList.SelectedIndex = _currentIndex;

        if (scrollToCurrent && _currentIndex >= 0 && _currentIndex < rows.Count)
            Dispatcher.UIThread.Post(() => TrackList.ScrollIntoView(rows[_currentIndex]));
    }

    private async Task PlayTrackAtAsync(int index, bool isAutomaticTransition = false)
    {
        if (index < 0 || index >= _filteredTracks.Count)
            return;

        var track = _filteredTracks[index];
        var path = _loadedLibrary.TrackPath(track);
        if (!File.Exists(path))
        {
            SetStatus($"Missing file: {path}");
            return;
        }

        try
        {
            await _audio.PlayAsync(path);
            _currentTrackFileName = track.FileName;
            _currentIndex = index;
            RefreshTrackRows(scrollToCurrent: true);
            NowPlayingText.Text = track.Title;
            var playerMetadata = string.Join(" · ", new[] { track.ChannelName, track.GenreText }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
            NowPlayingMetaText.Text = string.IsNullOrWhiteSpace(playerMetadata) ? "Local track" : playerMetadata;
            UpdateArtwork(track, isAutomaticTransition);
            UpdatePlayPauseIcon();
            UpdateReviewButton();
            UpdateMediaControls();
            SetStatus();
        }
        catch (Exception ex)
        {
            SetStatus($"Could not play {track.Title}: {ex.Message}");
            ShowToast("This track could not be played");
        }
    }

    private async void OnTrackTapped(object? sender, TappedEventArgs e)
    {
        await PlayTrackAtAsync(TrackList.SelectedIndex);
    }

    private async void OnTrackDoubleTapped(object? sender, RoutedEventArgs e)
    {
        await PlayTrackAtAsync(TrackList.SelectedIndex);
    }

    private async void OnPlayPauseClicked(object? sender, RoutedEventArgs e)
    {
        await TogglePlaybackAsync();
    }

    private async Task TogglePlaybackAsync()
    {
        if (CurrentTrack is null)
        {
            var index = TrackList.SelectedIndex >= 0 ? TrackList.SelectedIndex : 0;
            await PlayTrackAtAsync(index);
            return;
        }

        if (_audio.IsPlaying)
        {
            _audio.Pause();
            UpdatePlayPauseIcon();
            UpdateMediaControls();
        }
        else
        {
            _audio.Resume();
            UpdatePlayPauseIcon();
            UpdateMediaControls();
        }
    }

    private void OnPreviousClicked(object? sender, RoutedEventArgs e)
    {
        PlayPrevious();
    }

    private void PlayPrevious()
    {
        if (_filteredTracks.Count == 0)
            return;

        _ = PlayTrackAtAsync(Math.Max(0, _currentIndex - 1));
    }

    private void OnNextClicked(object? sender, RoutedEventArgs e)
    {
        PlayNext(isAutomaticTransition: false);
    }

    private void PlayNext(bool isAutomaticTransition = false)
    {
        if (_filteredTracks.Count == 0)
            return;

        var next = _currentIndex + 1;
        if (next >= _filteredTracks.Count)
        {
            _audio.Stop();
            _currentIndex = -1;
            _currentTrackFileName = null;
            NowPlayingText.Text = "Nothing playing";
            NowPlayingMetaText.Text = "Choose a track";
            NowPlayingCover.Source = null;
            ClearArtworkBackground();
            RefreshTrackRows();
            UpdatePlayPauseIcon();
            UpdateReviewButton();
            CompanionServices.MediaControls.Stop();
            return;
        }

        _ = PlayTrackAtAsync(next, isAutomaticTransition);
    }

    private void OnToggleSearchClicked(object? sender, RoutedEventArgs e)
    {
        SearchBox.IsVisible = !SearchBox.IsVisible;
        SearchButton.Opacity = SearchBox.IsVisible || !string.IsNullOrWhiteSpace(SearchBox.Text) ? 1 : 0.72;
        ToolTip.SetTip(SearchButton, SearchBox.IsVisible ? "Close search" : "Search");

        if (SearchBox.IsVisible)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
        else
        {
            TrackList.Focus();
        }
    }

    private void OnToggleFiltersClicked(object? sender, RoutedEventArgs e)
    {
        FilterDrawer.IsVisible = !FilterDrawer.IsVisible;
    }

    private void OnClearFiltersClicked(object? sender, RoutedEventArgs e)
    {
        _updatingPresetUi = true;
        PresetBox.SelectedIndex = -1;
        _updatingPresetUi = false;

        ApplyDefaultRatingFilter();
        RebuildFilterGroups();
        _showReviewOnly = false;
        SetReviewOnlyFilterVisual(false);
        ApplyFilter();
    }

    private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingPresetUi || PresetBox.SelectedItem is not string presetName)
            return;

        var preset = (_loadedLibrary.Library.FilterPresets ?? []).FirstOrDefault(p =>
            string.Equals(p.Name, presetName, StringComparison.OrdinalIgnoreCase));

        if (preset is null)
            return;

        ApplyFilterPreset(preset);
    }

    private void ApplyFilterPreset(PortableFilterPreset preset)
    {
        _filterGroups.Clear();
        FilterGroupsPanel.Children.Clear();

        var groups = preset.Groups
            .Where(group => group.Genres.Count > 0 || group.Styles.Count > 0 || (group.Tags?.Count ?? 0) > 0)
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
                controls.GenreFilter.SetSelectedItems(group.Genres, notify: false);
                controls.StyleFilter.SetSelectedItems(group.Styles, notify: false);
                controls.TagFilter.SetSelectedItems(group.Tags ?? [], notify: false);
                controls.NegateBox.IsChecked = group.Negate;
            }
        }

        ApplyFilter();
    }

    private void OnAddFilterGroupClicked(object? sender, RoutedEventArgs e)
    {
        AddFilterGroup();
        ApplyFilter();
    }

    private void RebuildFilterGroups()
    {
        _filterGroups.Clear();
        FilterGroupsPanel.Children.Clear();
        AddFilterGroup();
    }

    private FilterGroupControls AddFilterGroup()
    {
        var genreFilter = new MultiSelectFilterControl { Placeholder = "All genres" };
        genreFilter.SetItems(_loadedLibrary.Library.Genres);
        genreFilter.SelectionChanged += (_, _) => ApplyFilter();

        var styleFilter = new MultiSelectFilterControl { Placeholder = "All styles" };
        styleFilter.SetItems(_loadedLibrary.Library.Styles);
        styleFilter.SelectionChanged += (_, _) => ApplyFilter();

        var tagFilter = new MultiSelectFilterControl { Placeholder = "All tags" };
        tagFilter.SetItems(_loadedLibrary.Library.Tags);
        tagFilter.SelectionChanged += (_, _) => ApplyFilter();

        var negateBox = new CheckBox
        {
            Content = "Exclude matches",
            FontSize = 11,
            Opacity = 0.72
        };
        negateBox.IsCheckedChanged += (_, _) => ApplyFilter();

        var container = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(0, _filterGroups.Count == 0 ? 0 : 14, 0, 6) };
        var controls = new FilterGroupControls(genreFilter, styleFilter, tagFilter, negateBox, container);
        _filterGroups.Add(controls);

        if (_filterGroups.Count > 1)
        {
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var label = new TextBlock
            {
                Text = $"Group {_filterGroups.Count}",
                FontSize = 11,
                Opacity = 0.45,
                VerticalAlignment = VerticalAlignment.Center
            };
            var removeButton = new Button
            {
                Content = "x",
                Width = 34,
                Height = 30,
                Padding = new Avalonia.Thickness(0),
                FontSize = 11,
                Opacity = 0.55,
                Background = null,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            removeButton.Click += (_, _) => RemoveFilterGroup(controls);

            Grid.SetColumn(label, 0);
            Grid.SetColumn(removeButton, 1);
            header.Children.Add(label);
            header.Children.Add(removeButton);
            container.Children.Add(header);
        }

        container.Children.Add(negateBox);
        container.Children.Add(FilterSection("Genre", genreFilter));
        container.Children.Add(FilterSection("Style", styleFilter));
        container.Children.Add(FilterSection("Tags", tagFilter));
        FilterGroupsPanel.Children.Add(container);
        return controls;
    }

    private static StackPanel FilterSection(string label, Control control) =>
        new()
        {
            Spacing = 5,
            Children =
            {
                new TextBlock { Text = label, FontSize = 11, Opacity = 0.55 },
                control
            }
        };

    private void RemoveFilterGroup(FilterGroupControls controls)
    {
        var index = _filterGroups.IndexOf(controls);
        if (index < 0)
            return;

        _filterGroups.RemoveAt(index);
        FilterGroupsPanel.Children.Remove(controls.Container);
        if (_filterGroups.Count == 0)
            AddFilterGroup();

        ApplyFilter();
    }

    private void UpdateFilterCounts()
    {
        foreach (var group in _filterGroups)
        {
            var groupTracks = TracksMatchingSearchRatingAndGroup(group);
            var genreCounts = CountByName(groupTracks.SelectMany(track => track.Genres));
            var styleCounts = CountByName(groupTracks.SelectMany(track => track.Styles));
            var tagCounts = CountByName(groupTracks.SelectMany(track => track.Tags ?? []));

            group.GenreFilter.UpdateCounts(genreCounts);
            group.StyleFilter.UpdateCounts(styleCounts);
            group.TagFilter.UpdateCounts(tagCounts);
        }
    }

    private List<PortableTrack> TracksMatchingSearchRatingAndGroup(FilterGroupControls group)
    {
        IEnumerable<PortableTrack> query = _loadedLibrary.Library.Tracks;
        var term = SearchBox.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(track => track.Title.Contains(term, StringComparison.OrdinalIgnoreCase));

        if (RatingFilter.SelectedItems.Count > 0)
            query = query.Where(track => RatingFilter.SelectedItems.Contains(track.Rating));

        if (group.GenreFilter.SelectedItems.Count > 0)
            query = query.Where(track => group.GenreFilter.SelectedItems
                .All(genre => track.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase)));

        if (group.StyleFilter.SelectedItems.Count > 0)
            query = query.Where(track => group.StyleFilter.SelectedItems
                .All(style => track.Styles.Contains(style, StringComparer.OrdinalIgnoreCase)));

        if (group.TagFilter.SelectedItems.Count > 0)
            query = query.Where(track => group.TagFilter.SelectedItems
                .All(tag => (track.Tags ?? []).Contains(tag, StringComparer.OrdinalIgnoreCase)));

        return query.ToList();
    }

    private static Dictionary<string, int> CountByName(IEnumerable<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            counts[value] = counts.GetValueOrDefault(value, 0) + 1;
        return counts;
    }

    private Bitmap? LoadThumbnail(PortableTrack track)
    {
        var key = track.FileName;
        if (_thumbnailCache.TryGetValue(key, out var cached))
            return cached;

        Bitmap? bitmap = null;
        try
        {
            if (track.Thumbnail is { Length: > 0 } thumbnail)
            {
                using var stream = new MemoryStream(thumbnail, writable: false);
                bitmap = Bitmap.DecodeToWidth(stream, 96, BitmapInterpolationMode.MediumQuality);
            }
            else if (_loadedLibrary.CoverPath(track) is { } path)
            {
                using var stream = File.OpenRead(path);
                bitmap = Bitmap.DecodeToWidth(stream, 96, BitmapInterpolationMode.MediumQuality);
            }
        }
        catch
        {
            bitmap = null;
        }

        AddThumbnailToCache(key, bitmap);
        return bitmap;
    }

    private void AddThumbnailToCache(string key, Bitmap? bitmap)
    {
        while (_thumbnailCache.Count >= ThumbnailCacheCapacity && _thumbnailCacheOrder.TryDequeue(out var oldestKey))
        {
            if (_thumbnailCache.Remove(oldestKey, out var oldest))
                oldest?.Dispose();
        }

        _thumbnailCache[key] = bitmap;
        _thumbnailCacheOrder.Enqueue(key);
    }

    private void ClearThumbnailCache()
    {
        foreach (var cover in _thumbnailCache.Values)
            cover?.Dispose();

        _thumbnailCache.Clear();
        _thumbnailCacheOrder.Clear();
    }

    private void UpdateArtwork(PortableTrack track, bool isAutomaticTransition)
    {
        if (string.Equals(_activeArtworkFileName, track.FileName, StringComparison.OrdinalIgnoreCase))
        {
            NowPlayingCover.Source = _activeArtwork;
            return;
        }

        PrepareOutgoingArtwork();
        var loaded = LoadArtwork(track);
        _activeArtwork = loaded.Bitmap;
        _activeArtworkFileName = track.FileName;
        _activeAmbientPalette = loaded.Palette;

        AppArtworkBackground.Source = _activeArtwork;
        AppArtworkBackground.IsVisible = _activeArtwork is not null;
        AppArtworkBackground.Opacity = 0;
        PlayerArtworkBackground.Source = _activeArtwork;
        PlayerArtworkBackground.IsVisible = _activeArtwork is not null;
        PlayerArtworkBackground.Opacity = 0;
        NowPlayingCover.Source = _activeArtwork;

        _artworkTransitionDuration = isAutomaticTransition
            ? AutomaticArtworkTransitionDuration
            : ManualArtworkTransitionDuration;
        _artworkTransitionProgress = _fadedArtwork is null ? 1 : 0;
        _artworkTransitionStartedAt = DateTimeOffset.UtcNow;
        ApplyArtworkTransitionFrame();

        if (_fadedArtwork is not null)
            _artworkTimer.Start();
    }

    private void PrepareOutgoingArtwork()
    {
        AppArtworkPreviousBackground.Source = null;
        PlayerArtworkPreviousBackground.Source = null;
        _fadedArtwork?.Dispose();

        _fadedArtwork = _activeArtwork;
        _activeArtwork = null;
        _fadedAmbientPalette = _activeAmbientPalette;

        AppArtworkPreviousBackground.Source = _fadedArtwork;
        AppArtworkPreviousBackground.IsVisible = _fadedArtwork is not null;
        AppArtworkPreviousBackground.Opacity = AppArtworkBackground.Opacity;
        PlayerArtworkPreviousBackground.Source = _fadedArtwork;
        PlayerArtworkPreviousBackground.IsVisible = _fadedArtwork is not null;
        PlayerArtworkPreviousBackground.Opacity = PlayerArtworkBackground.Opacity;

        AppArtworkBackground.Source = null;
        AppArtworkBackground.IsVisible = false;
        AppArtworkBackground.Opacity = 0;
        PlayerArtworkBackground.Source = null;
        PlayerArtworkBackground.IsVisible = false;
        PlayerArtworkBackground.Opacity = 0;
    }

    private LoadedArtwork LoadArtwork(PortableTrack track)
    {
        try
        {
            Bitmap? bitmap = null;
            if (_loadedLibrary.CoverPath(track) is { } coverPath)
            {
                using var coverStream = File.OpenRead(coverPath);
                bitmap = Bitmap.DecodeToWidth(coverStream, 720, BitmapInterpolationMode.MediumQuality);
            }
            else if (track.Thumbnail is { Length: > 0 } thumbnail)
            {
                using var thumbnailStream = new MemoryStream(thumbnail, writable: false);
                bitmap = Bitmap.DecodeToWidth(thumbnailStream, 480, BitmapInterpolationMode.MediumQuality);
            }

            return new LoadedArtwork(bitmap, ExtractAmbientPalette(track.Thumbnail));
        }
        catch
        {
            return new LoadedArtwork(null, ExtractAmbientPalette(track.Thumbnail));
        }
    }

    private void UpdateArtworkTransition()
    {
        if (_fadedArtwork is null)
        {
            _artworkTimer.Stop();
            return;
        }

        _artworkTransitionProgress = Math.Clamp(
            (DateTimeOffset.UtcNow - _artworkTransitionStartedAt).TotalMilliseconds
            / _artworkTransitionDuration.TotalMilliseconds,
            0,
            1);
        ApplyArtworkTransitionFrame();

        if (_artworkTransitionProgress < 1)
            return;

        AppArtworkPreviousBackground.Source = null;
        AppArtworkPreviousBackground.IsVisible = false;
        PlayerArtworkPreviousBackground.Source = null;
        PlayerArtworkPreviousBackground.IsVisible = false;
        _fadedArtwork.Dispose();
        _fadedArtwork = null;
        _artworkTimer.Stop();
    }

    private void ApplyArtworkTransitionFrame()
    {
        var progress = SmoothStep(_artworkTransitionProgress);
        var incoming = _fadedArtwork is null ? 1 : Math.Sin(progress * Math.PI / 2);
        var outgoing = _fadedArtwork is null ? 0 : Math.Cos(progress * Math.PI / 2);

        AppArtworkBackground.Opacity = AppArtworkBackground.IsVisible ? 0.46 * incoming : 0;
        AppArtworkPreviousBackground.Opacity = AppArtworkPreviousBackground.IsVisible ? 0.46 * outgoing : 0;
        PlayerArtworkBackground.Opacity = PlayerArtworkBackground.IsVisible ? 0.58 * incoming : 0;
        PlayerArtworkPreviousBackground.Opacity = PlayerArtworkPreviousBackground.IsVisible ? 0.58 * outgoing : 0;

        var palette = new AmbientPalette(
            MixColor(_fadedAmbientPalette.Primary, _activeAmbientPalette.Primary, progress),
            MixColor(_fadedAmbientPalette.Secondary, _activeAmbientPalette.Secondary, progress));
        var appStops = ((LinearGradientBrush)AppAtmosphereTint.Background!).GradientStops;
        var playerStops = ((LinearGradientBrush)PlayerAtmosphereTint.Background!).GradientStops;
        appStops[0].Color = WithAlpha(palette.Primary, 72);
        appStops[2].Color = WithAlpha(palette.Secondary, 56);
        playerStops[0].Color = WithAlpha(palette.Primary, 112);
        playerStops[2].Color = WithAlpha(palette.Secondary, 88);
    }

    private void ClearArtworkBackground()
    {
        _artworkTimer.Stop();
        NowPlayingCover.Source = null;
        AppArtworkBackground.Source = null;
        AppArtworkBackground.IsVisible = false;
        AppArtworkPreviousBackground.Source = null;
        AppArtworkPreviousBackground.IsVisible = false;
        PlayerArtworkBackground.Source = null;
        PlayerArtworkBackground.IsVisible = false;
        PlayerArtworkPreviousBackground.Source = null;
        PlayerArtworkPreviousBackground.IsVisible = false;
        _activeArtwork?.Dispose();
        _fadedArtwork?.Dispose();
        _activeArtwork = null;
        _fadedArtwork = null;
        _activeArtworkFileName = null;
        _artworkTransitionProgress = 1;
    }

    private static AmbientPalette ExtractAmbientPalette(byte[]? thumbnail)
    {
        if (thumbnail is not { Length: > 0 })
            return DefaultAmbientPalette;

        try
        {
            using var bitmap = SKBitmap.Decode(thumbnail);
            if (bitmap is null || bitmap.Width == 0 || bitmap.Height == 0)
                return DefaultAmbientPalette;

            double red = 0, green = 0, blue = 0, weightTotal = 0;
            var step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 24);
            for (var y = step / 2; y < bitmap.Height; y += step)
            for (var x = step / 2; x < bitmap.Width; x += step)
            {
                var color = bitmap.GetPixel(x, y);
                color.ToHsl(out _, out var sampleSaturation, out var lightness);
                if (color.Alpha < 150 || lightness < 8 || lightness > 90)
                    continue;

                var weight = 0.25 + sampleSaturation / 100d;
                red += color.Red * weight;
                green += color.Green * weight;
                blue += color.Blue * weight;
                weightTotal += weight;
            }

            if (weightTotal <= 0)
                return DefaultAmbientPalette;

            var average = new SKColor(
                (byte)Math.Clamp(red / weightTotal, 0, 255),
                (byte)Math.Clamp(green / weightTotal, 0, 255),
                (byte)Math.Clamp(blue / weightTotal, 0, 255));
            average.ToHsl(out var hue, out var averageSaturation, out _);
            var primary = SKColor.FromHsl(hue, Math.Clamp(averageSaturation * 1.28f, 58, 84), 50);
            var secondary = SKColor.FromHsl((hue + 48) % 360, Math.Clamp(averageSaturation * 1.12f, 48, 76), 43);
            return new AmbientPalette(
                Color.FromRgb(primary.Red, primary.Green, primary.Blue),
                Color.FromRgb(secondary.Red, secondary.Green, secondary.Blue));
        }
        catch
        {
            return DefaultAmbientPalette;
        }
    }

    private static double SmoothStep(double progress)
    {
        progress = Math.Clamp(progress, 0, 1);
        return progress * progress * (3 - 2 * progress);
    }

    private static Color MixColor(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    private sealed record LoadedArtwork(Bitmap? Bitmap, AmbientPalette Palette);
    private sealed record AmbientPalette(Color Primary, Color Secondary);

    private async void OnShuffleClicked(object? sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        ShuffleButton.Opacity = _shuffle ? 1.0 : 0.45;
        ToolTip.SetTip(ShuffleButton, _shuffle ? "Shuffle: On" : "Shuffle: Off");
        ApplyFilter();

        if (_filteredTracks.Count > 0)
            await PlayTrackAtAsync(0);
    }

    private async void OnReviewClicked(object? sender, RoutedEventArgs e)
    {
        var track = CurrentTrack;
        if (track is null)
        {
            ShowToast("No active track");
            return;
        }

        await SetTrackReviewAsync(track.FileName, !track.NeedsReview);
        ShowToast(track.NeedsReview ? "Review mark removed" : "Marked for review");
        ApplyFilter();
        UpdateReviewButton();
    }

    private async Task SetTrackReviewAsync(string fileName, bool needsReview)
    {
        var tracks = _loadedLibrary.Library.Tracks
            .Select(track => string.Equals(track.FileName, fileName, StringComparison.OrdinalIgnoreCase)
                ? track with { NeedsReview = needsReview }
                : track)
            .ToList();

        _loadedLibrary = _loadedLibrary with
        {
            Library = _loadedLibrary.Library with { Tracks = tracks }
        };

        await PortableLibraryStore.SaveAsync(
            CompanionServices.LibraryStorage.LibraryDirectory,
            _loadedLibrary.Library);
    }

    private void UpdateReviewButton()
    {
        var isMarked = CurrentTrack?.NeedsReview == true;

        ReviewButton.Opacity = isMarked ? 1.0 : 0.45;
        ToolTip.SetTip(ReviewButton, isMarked ? "Remove review mark" : "Mark for review");
    }

    private void OnReviewFilterClicked(object? sender, RoutedEventArgs e)
    {
        _showReviewOnly = !_showReviewOnly;
        SetReviewOnlyFilterVisual(_showReviewOnly);
        ApplyFilter();
        UpdateReviewFilterButton();
    }

    private void OnReviewOnlyFilterChanged(object? sender, RoutedEventArgs e)
    {
        if (_updatingReviewFilterUi)
            return;

        _showReviewOnly = ReviewOnlyFilterBox.IsChecked == true;
        ApplyFilter();
        UpdateReviewFilterButton();
    }

    private void SetReviewOnlyFilterVisual(bool value)
    {
        _updatingReviewFilterUi = true;
        ReviewOnlyFilterBox.IsChecked = value;
        _updatingReviewFilterUi = false;
    }

    private void UpdateReviewFilterButton()
    {
        var count = _loadedLibrary.Library.Tracks.Count(track => track.NeedsReview);
        ReviewFilterButton.Opacity = _showReviewOnly ? 1.0 : 0.45;
        ToolTip.SetTip(ReviewFilterButton, _showReviewOnly
            ? $"Review filter: On ({count})"
            : $"Reviews ({count})");
    }

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider.CanPickFolder != true)
        {
            SetStatus("Folder import is not available on this device.");
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select exported MusicLibrary zip",
            AllowMultiple = false
        });
        ConfigureSystemBars();

        if (files.Count > 0)
        {
            await ImportLibraryArchiveAsync(files[0]);
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select exported MusicLibrary folder",
            AllowMultiple = false
        });
        ConfigureSystemBars();

        if (folders.Count == 0)
            return;

        await ImportLibraryAsync(folders[0]);
    }

    private async Task ImportLibraryArchiveAsync(IStorageFile selectedFile)
    {
        ImportButton.IsEnabled = false;
        SetStatus();

        var targetDirectory = CompanionServices.LibraryStorage.LibraryDirectory;
        var tempDirectory = targetDirectory + ".archive-import";

        try
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            Directory.CreateDirectory(tempDirectory);
            await using (var source = await selectedFile.OpenReadAsync())
            using (var archive = new ZipArchive(source, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                    ExtractArchiveEntry(entry, tempDirectory);
            }

            await ImportLibraryDirectoryAsync(tempDirectory);
        }
        catch (Exception ex)
        {
            SetStatus($"Import failed: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            ImportButton.IsEnabled = true;
        }
    }

    private async Task ImportLibraryAsync(IStorageFolder selectedFolder)
    {
        ImportButton.IsEnabled = false;
        SetStatus();
        var tempDirectory = CompanionServices.LibraryStorage.LibraryDirectory + ".import";

        try
        {
            var sourceFolder = await FindLibraryFolderAsync(selectedFolder);
            if (sourceFolder is null)
            {
                SetStatus("Import folder must contain library.json and a tracks folder.");
                return;
            }

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            Directory.CreateDirectory(tempDirectory);
            await CopyFolderAsync(sourceFolder, tempDirectory);

            await ImportLibraryDirectoryAsync(tempDirectory);
        }
        catch (Exception ex)
        {
            SetStatus($"Import failed: {ex.Message}");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            ImportButton.IsEnabled = true;
        }
    }

    private async Task ImportLibraryDirectoryAsync(string sourceDirectory)
    {
        if (!File.Exists(Path.Combine(sourceDirectory, PortableLibraryStore.FileName)))
        {
            SetStatus("Import must contain library.json.");
            return;
        }

        await PortableLibraryStore.LoadAsync(sourceDirectory);

        var targetDirectory = CompanionServices.LibraryStorage.LibraryDirectory;
        Directory.CreateDirectory(targetDirectory);

        _audio.Stop();
        File.Copy(
            Path.Combine(sourceDirectory, PortableLibraryStore.FileName),
            Path.Combine(targetDirectory, PortableLibraryStore.FileName),
            overwrite: true);

        CopyDirectoryIfExists(Path.Combine(sourceDirectory, "tracks"), Path.Combine(targetDirectory, "tracks"));
        CopyDirectoryIfExists(Path.Combine(sourceDirectory, "covers"), Path.Combine(targetDirectory, "covers"));

        await LoadLibraryAsync();
        ShowToast($"Library imported · {_loadedLibrary.Library.Tracks.Count} tracks · {ImportedLibraryDuration()}");
    }

    private static async Task<IStorageFolder?> FindLibraryFolderAsync(IStorageFolder selectedFolder)
    {
        var items = await GetItemsAsync(selectedFolder);
        if (HasLibraryFiles(items))
            return selectedFolder;

        return items
            .OfType<IStorageFolder>()
            .FirstOrDefault(folder => string.Equals(folder.Name, "MusicLibrary", StringComparison.OrdinalIgnoreCase));
    }

    private string ImportedLibraryDuration()
    {
        var totalSeconds = _loadedLibrary.Library.Tracks
            .Select(track => track.DurationSeconds ?? 0)
            .Sum();

        return FormatPlaylistDuration(totalSeconds);
    }

    private async void ShowToast(string message)
    {
        _toastCts?.Cancel();
        _toastCts = new CancellationTokenSource();
        var token = _toastCts.Token;

        ToastText.Text = message;
        Toast.IsVisible = true;

        try
        {
            await Task.Delay(2400, token);
            if (!token.IsCancellationRequested)
                Toast.IsVisible = false;
        }
        catch (TaskCanceledException)
        {
        }
    }

    private static bool HasLibraryFiles(IReadOnlyList<IStorageItem> items) =>
        items.OfType<IStorageFile>().Any(file => string.Equals(file.Name, PortableLibraryStore.FileName, StringComparison.OrdinalIgnoreCase));

    private static async Task CopyFolderAsync(IStorageFolder sourceFolder, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var item in await GetItemsAsync(sourceFolder))
        {
            var targetPath = Path.Combine(targetDirectory, Path.GetFileName(item.Name));
            switch (item)
            {
                case IStorageFile file:
                    await CopyFileAsync(file, targetPath);
                    break;
                case IStorageFolder folder:
                    await CopyFolderAsync(folder, targetPath);
                    break;
            }
        }
    }

    private static async Task CopyFileAsync(IStorageFile file, string targetPath)
    {
        await using var source = await file.OpenReadAsync();
        await using var target = File.Create(targetPath);
        await source.CopyToAsync(target);
    }

    private static async Task<List<IStorageItem>> GetItemsAsync(IStorageFolder folder)
    {
        var items = new List<IStorageItem>();
        await foreach (var item in folder.GetItemsAsync())
            items.Add(item);

        return items;
    }

    private static void CopyDirectoryIfExists(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory))
            return;

        Directory.CreateDirectory(targetDirectory);
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            var targetParent = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetParent))
                Directory.CreateDirectory(targetParent);

            File.Copy(sourceFile, targetFile, overwrite: true);
        }
    }

    private static void ExtractArchiveEntry(ZipArchiveEntry entry, string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(entry.FullName))
            return;

        var destinationPath = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
        var targetRoot = Path.GetFullPath(targetDirectory);
        var rootPrefix = targetRoot.EndsWith(Path.DirectorySeparatorChar)
            ? targetRoot
            : targetRoot + Path.DirectorySeparatorChar;
        if (!destinationPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Archive contains an invalid path.");

        if (entry.FullName.EndsWith("/", StringComparison.Ordinal) ||
            entry.FullName.EndsWith("\\", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(destinationPath);
            return;
        }

        var parent = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        entry.ExtractToFile(destinationPath, overwrite: true);
    }

    private void OnProgressPressed(object? sender, PointerPressedEventArgs e)
    {
        _isSeeking = true;
    }

    private void OnProgressReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_audio.Duration.TotalSeconds > 0)
        {
            var seconds = _audio.Duration.TotalSeconds * ProgressSlider.Value / 100.0;
            SeekTo(TimeSpan.FromSeconds(seconds));
        }

        _isSeeking = false;
    }

    private void SeekTo(TimeSpan position)
    {
        _audio.Seek(position);
        UpdatePlaybackUi();
        UpdateMediaControls();
    }

    private void UpdatePlaybackUi()
    {
        if (!_isSeeking && _audio.Duration.TotalSeconds > 0)
            ProgressSlider.Value = _audio.Position.TotalSeconds / _audio.Duration.TotalSeconds * 100.0;

        TimeText.Text = $"{Format(_audio.Position)} / {Format(_audio.Duration)}";
        if (CurrentTrack is not null)
        {
            UpdatePlayPauseIcon();
            if (_audio.IsPlaying && DateTime.UtcNow - _lastMediaUpdate > TimeSpan.FromSeconds(4))
                UpdateMediaControls();
        }

        UpdatePlaylistSummary();
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";

    private void UpdatePlaylistSummary()
    {
        var totalSeconds = _filteredTracks
            .Select(track => track.DurationSeconds ?? 0)
            .Sum();

        PlaylistSummaryText.Text = $"{_filteredTracks.Count} tracks · {FormatPlaylistDuration(totalSeconds)}";
    }

    private static string FormatPlaylistDuration(int totalSeconds)
    {
        var time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}h"
            : $"{time.Minutes}:{time.Seconds:D2}m";
    }

    private void SetStatus(string? message = null)
    {
        StatusText.Text = message ?? string.Empty;
        StatusText.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private void ShuffleFilteredTracks()
    {
        for (var i = _filteredTracks.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (_filteredTracks[i], _filteredTracks[j]) = (_filteredTracks[j], _filteredTracks[i]);
        }
    }

    private void UpdatePlayPauseIcon()
    {
        PlayIcon.IsVisible = !_audio.IsPlaying;
        PauseIcon.IsVisible = _audio.IsPlaying;
    }

    private void UpdateMediaControls()
    {
        var track = CurrentTrack;
        if (track is null)
        {
            CompanionServices.MediaControls.Stop();
            return;
        }

        CompanionServices.MediaControls.Update(
            track.Title,
            _loadedLibrary.CoverPath(track),
            _audio.IsPlaying,
            _audio.Position,
            _audio.Duration);
        _lastMediaUpdate = DateTime.UtcNow;
    }

    private PortableTrack? CurrentTrack => string.IsNullOrWhiteSpace(_currentTrackFileName)
        ? null
        : _loadedLibrary.Library.Tracks.FirstOrDefault(track => string.Equals(
            track.FileName,
            _currentTrackFileName,
            StringComparison.OrdinalIgnoreCase));

    private void OnMediaCommandRequested(MediaControlCommand command)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            switch (command)
            {
                case MediaControlCommand.Previous:
                    PlayPrevious();
                    break;
                case MediaControlCommand.PlayPause:
                    await TogglePlaybackAsync();
                    break;
                case MediaControlCommand.Next:
                    PlayNext();
                    break;
            }
        });
    }

    private void OnMediaSeekRequested(TimeSpan position)
    {
        Dispatcher.UIThread.Post(() => SeekTo(position));
    }
}
