using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Music.Core;

namespace Music.Companion.Views;

public partial class MainView : UserControl
{
    private readonly ICompanionAudioPlayer _audio = CompanionServices.AudioPlayer;
    private readonly DispatcherTimer _timer;

    private LoadedMusicLibrary _loadedLibrary = new("", PortableMusicLibrary.Empty);

    private List<PortableTrack> _filteredTracks = [];
    private readonly List<FilterGroupControls> _filterGroups = [];
    private readonly Dictionary<string, Bitmap?> _coverCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _rng = new();
    private int _currentIndex = -1;
    private bool _isSeeking;
    private bool _shuffle;
    private bool _updatingPresetUi;
    private DateTime _lastMediaUpdate = DateTime.MinValue;
    private CancellationTokenSource? _toastCts;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreFilter,
        MultiSelectFilterControl StyleFilter,
        StackPanel Container);

    private sealed record TrackRow(PortableTrack Track, Bitmap? Cover, bool IsCurrent)
    {
        private static readonly IBrush CurrentBackgroundBrush = new SolidColorBrush(Color.FromArgb(78, 17, 121, 184));
        private static readonly IBrush CurrentAccentBrush = new SolidColorBrush(Color.FromRgb(31, 154, 240));
        private static readonly IBrush TransparentBrush = Brushes.Transparent;

        public string Title => Track.Title;
        public string GenreText => Track.GenreText;
        public string StyleText => Track.StyleText;
        public string DurationText => Track.DurationText;
        public string Rating => Track.Rating;
        public IBrush CurrentBackground => IsCurrent ? CurrentBackgroundBrush : TransparentBrush;
        public IBrush CurrentAccent => IsCurrent ? CurrentAccentBrush : TransparentBrush;
    }

    public MainView()
    {
        InitializeComponent();

        SearchBox.TextChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();

        ProgressSlider.AddHandler(PointerPressedEvent, OnProgressPressed, RoutingStrategies.Tunnel);
        ProgressSlider.AddHandler(PointerReleasedEvent, OnProgressReleased, RoutingStrategies.Tunnel);

        _audio.PlaybackEnded += () => Dispatcher.UIThread.Post(PlayNext);
        CompanionServices.MediaControls.CommandRequested += OnMediaCommandRequested;
        CompanionServices.MediaControls.SeekRequested += OnMediaSeekRequested;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
        _timer.Start();

        _ = LoadLibraryAsync();
    }

    private async Task LoadLibraryAsync()
    {
        try
        {
            ClearCoverCache();
            _loadedLibrary = await PortableLibraryStore.LoadAsync(CompanionServices.LibraryStorage.LibraryDirectory);
            StatusText.Text = "";
        }
        catch (Exception ex)
        {
            _loadedLibrary = new LoadedMusicLibrary(
                CompanionServices.LibraryStorage.LibraryDirectory,
                PortableMusicLibrary.Empty);
            StatusText.Text = $"Could not load library: {ex.Message}";
        }

        PopulateFilters();
        ApplyFilter();
    }

    private void PopulateFilters()
    {
        PopulatePresets();
        RatingFilter.SetItems(_loadedLibrary.Library.Ratings);
        RebuildFilterGroups();
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
                    group.StyleFilter.SelectedItems.ToList()))
                .ToList());

        if (_shuffle)
            ShuffleFilteredTracks();

        if (_currentIndex >= _filteredTracks.Count)
            _currentIndex = -1;

        RefreshTrackRows();
        UpdatePlaylistSummary();
        UpdateFilterCounts();
    }

    private void RefreshTrackRows(bool scrollToCurrent = false)
    {
        var rows = _filteredTracks
            .Select((track, index) => new TrackRow(track, LoadCover(track), index == _currentIndex))
            .ToList();

        TrackList.ItemsSource = rows;
        TrackList.SelectedIndex = _currentIndex;

        if (scrollToCurrent && _currentIndex >= 0 && _currentIndex < rows.Count)
            Dispatcher.UIThread.Post(() => TrackList.ScrollIntoView(rows[_currentIndex]));
    }

    private async Task PlayTrackAtAsync(int index)
    {
        if (index < 0 || index >= _filteredTracks.Count)
            return;

        var track = _filteredTracks[index];
        var path = _loadedLibrary.TrackPath(track);
        if (!File.Exists(path))
        {
            StatusText.Text = $"Missing file: {path}";
            return;
        }

        _currentIndex = index;
        RefreshTrackRows(scrollToCurrent: true);
        NowPlayingText.Text = track.Title;
        await _audio.PlayAsync(path);
        UpdatePlayPauseIcon();
        UpdateMediaControls();
        StatusText.Text = "";
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
        if (_currentIndex < 0)
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
        PlayNext();
    }

    private void PlayNext()
    {
        if (_filteredTracks.Count == 0)
            return;

        var next = _currentIndex + 1;
        if (next >= _filteredTracks.Count)
        {
            _audio.Stop();
            _currentIndex = -1;
            NowPlayingText.Text = "";
            RefreshTrackRows();
            UpdatePlayPauseIcon();
            CompanionServices.MediaControls.Stop();
            return;
        }

        _ = PlayTrackAtAsync(next);
    }

    private void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        _ = LoadLibraryAsync();
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

        RatingFilter.SetItems(_loadedLibrary.Library.Ratings);
        RebuildFilterGroups();
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
                controls.GenreFilter.SetSelectedItems(group.Genres, notify: false);
                controls.StyleFilter.SetSelectedItems(group.Styles, notify: false);
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

        var container = new StackPanel { Spacing = 8, Margin = new Avalonia.Thickness(0, _filterGroups.Count == 0 ? 0 : 14, 0, 6) };
        var controls = new FilterGroupControls(genreFilter, styleFilter, container);
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

        container.Children.Add(FilterSection("Genre", genreFilter));
        container.Children.Add(FilterSection("Style", styleFilter));
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

            group.GenreFilter.UpdateCounts(genreCounts);
            group.StyleFilter.UpdateCounts(styleCounts);
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

        return query.ToList();
    }

    private static Dictionary<string, int> CountByName(IEnumerable<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            counts[value] = counts.GetValueOrDefault(value, 0) + 1;
        return counts;
    }

    private Bitmap? LoadCover(PortableTrack track)
    {
        var path = _loadedLibrary.CoverPath(track);
        if (path is null)
            return null;

        if (_coverCache.TryGetValue(path, out var cached))
            return cached;

        try
        {
            var bitmap = new Bitmap(path);
            _coverCache[path] = bitmap;
            return bitmap;
        }
        catch
        {
            _coverCache[path] = null;
            return null;
        }
    }

    private void ClearCoverCache()
    {
        foreach (var cover in _coverCache.Values)
            cover?.Dispose();

        _coverCache.Clear();
    }

    private async void OnShuffleClicked(object? sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        ShuffleButton.Opacity = _shuffle ? 1.0 : 0.45;
        ToolTip.SetTip(ShuffleButton, _shuffle ? "Shuffle: On" : "Shuffle: Off");
        ApplyFilter();

        if (_filteredTracks.Count > 0)
            await PlayTrackAtAsync(0);
    }

    private async void OnImportClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider.CanPickFolder != true)
        {
            StatusText.Text = "Folder import is not available on this device.";
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select exported MusicLibrary folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
            return;

        await ImportLibraryAsync(folders[0]);
    }

    private async Task ImportLibraryAsync(IStorageFolder selectedFolder)
    {
        ImportButton.IsEnabled = false;
        ReloadButton.IsEnabled = false;
        StatusText.Text = "";

        try
        {
            var sourceFolder = await FindLibraryFolderAsync(selectedFolder);
            if (sourceFolder is null)
            {
                StatusText.Text = "Import folder must contain library.json and a tracks folder.";
                return;
            }

            var targetDirectory = CompanionServices.LibraryStorage.LibraryDirectory;
            var tempDirectory = targetDirectory + ".import";

            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);

            Directory.CreateDirectory(tempDirectory);
            await CopyFolderAsync(sourceFolder, tempDirectory);

            if (!File.Exists(Path.Combine(tempDirectory, PortableLibraryStore.FileName)) ||
                !Directory.Exists(Path.Combine(tempDirectory, "tracks")))
            {
                Directory.Delete(tempDirectory, true);
                StatusText.Text = "Import folder must contain library.json and a tracks folder.";
                return;
            }

            await PortableLibraryStore.LoadAsync(tempDirectory);

            _audio.Stop();
            if (Directory.Exists(targetDirectory))
                Directory.Delete(targetDirectory, true);

            Directory.Move(tempDirectory, targetDirectory);
            await LoadLibraryAsync();
            ShowToast($"Library imported · {_loadedLibrary.Library.Tracks.Count} tracks · {ImportedLibraryDuration()}");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Import failed: {ex.Message}";
        }
        finally
        {
            ImportButton.IsEnabled = true;
            ReloadButton.IsEnabled = true;
        }
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
        items.OfType<IStorageFile>().Any(file => string.Equals(file.Name, PortableLibraryStore.FileName, StringComparison.OrdinalIgnoreCase)) &&
        items.OfType<IStorageFolder>().Any(folder => string.Equals(folder.Name, "tracks", StringComparison.OrdinalIgnoreCase));

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
        if (_currentIndex >= 0)
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

        var summary = $"{_filteredTracks.Count} tracks · {FormatPlaylistDuration(totalSeconds)}";
        var remaining = RemainingPlaylistDurationSeconds();
        if (remaining > 0)
            summary += $" · ends {DateTime.Now.AddSeconds(remaining):HH:mm}";

        PlaylistSummaryText.Text = summary;
    }

    private int RemainingPlaylistDurationSeconds()
    {
        if (_currentIndex < 0 || _currentIndex >= _filteredTracks.Count)
            return 0;

        var currentRemaining = _audio.Duration.TotalSeconds > 0
            ? Math.Max(0, (int)(_audio.Duration - _audio.Position).TotalSeconds)
            : _filteredTracks[_currentIndex].DurationSeconds ?? 0;

        return currentRemaining + _filteredTracks
            .Skip(_currentIndex + 1)
            .Select(track => track.DurationSeconds ?? 0)
            .Sum();
    }

    private static string FormatPlaylistDuration(int totalSeconds)
    {
        var time = TimeSpan.FromSeconds(totalSeconds);
        return time.TotalHours >= 1
            ? $"{(int)time.TotalHours}:{time.Minutes:D2}h"
            : $"{time.Minutes}:{time.Seconds:D2}m";
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
        if (_currentIndex < 0 || _currentIndex >= _filteredTracks.Count)
        {
            CompanionServices.MediaControls.Stop();
            return;
        }

        var track = _filteredTracks[_currentIndex];
        CompanionServices.MediaControls.Update(
            track.Title,
            _loadedLibrary.CoverPath(track),
            _audio.IsPlaying,
            _audio.Position,
            _audio.Duration);
        _lastMediaUpdate = DateTime.UtcNow;
    }

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
