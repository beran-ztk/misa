using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
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
    private readonly Random _rng = new();
    private int _currentIndex = -1;
    private bool _isSeeking;
    private bool _shuffle;

    private record FilterGroupControls(
        MultiSelectFilterControl GenreFilter,
        MultiSelectFilterControl StyleFilter,
        StackPanel Container);

    public MainView()
    {
        InitializeComponent();

        SearchBox.TextChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();

        ProgressSlider.AddHandler(PointerPressedEvent, OnProgressPressed, RoutingStrategies.Tunnel);
        ProgressSlider.AddHandler(PointerReleasedEvent, OnProgressReleased, RoutingStrategies.Tunnel);

        _audio.PlaybackEnded += () => Dispatcher.UIThread.Post(PlayNext);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
        _timer.Start();

        _ = LoadLibraryAsync();
    }

    private async Task LoadLibraryAsync()
    {
        try
        {
            _loadedLibrary = await PortableLibraryStore.LoadAsync(CompanionServices.LibraryStorage.LibraryDirectory);
            StatusText.Text = _loadedLibrary.Library.Tracks.Count == 0
                ? $"No library found. Put library.json and tracks/ into: {_loadedLibrary.RootDirectory}"
                : $"{_loadedLibrary.Library.Tracks.Count} tracks loaded from {_loadedLibrary.RootDirectory}";
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
        RatingFilter.SetItems(_loadedLibrary.Library.Ratings);
        RebuildFilterGroups();
    }

    private void ApplyFilter()
    {
        _filteredTracks = PortableTrackFilter.Apply(
            _loadedLibrary.Library.Tracks,
            SearchBox.Text,
            RatingFilter.SelectedItems,
            _filterGroups
                .Select(group => new PortableFilterGroup(group.GenreFilter.SelectedItems, group.StyleFilter.SelectedItems))
                .ToList());

        if (_shuffle)
            ShuffleFilteredTracks();

        TrackList.ItemsSource = _filteredTracks;
        if (_currentIndex >= _filteredTracks.Count)
            _currentIndex = -1;

        UpdateFilterCounts();
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
        TrackList.SelectedIndex = index;
        NowPlayingText.Text = track.Title;
        await _audio.PlayAsync(path);
        PlayPauseButton.Content = "II";
        StatusText.Text = "";
    }

    private async void OnTrackDoubleTapped(object? sender, RoutedEventArgs e)
    {
        await PlayTrackAtAsync(TrackList.SelectedIndex);
    }

    private async void OnPlayPauseClicked(object? sender, RoutedEventArgs e)
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
            PlayPauseButton.Content = ">";
        }
        else
        {
            _audio.Resume();
            PlayPauseButton.Content = "II";
        }
    }

    private void OnPreviousClicked(object? sender, RoutedEventArgs e)
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
            PlayPauseButton.Content = ">";
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
        RatingFilter.SetItems(_loadedLibrary.Library.Ratings);
        RebuildFilterGroups();
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

    private void AddFilterGroup()
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
                Padding = new Avalonia.Thickness(8, 2),
                FontSize = 11,
                Opacity = 0.55,
                Background = null
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
        var genreCounts = CountByName(_filteredTracks.SelectMany(track => track.Genres));
        var styleCounts = CountByName(_filteredTracks.SelectMany(track => track.Styles));

        foreach (var group in _filterGroups)
        {
            group.GenreFilter.UpdateCounts(genreCounts);
            group.StyleFilter.UpdateCounts(styleCounts);
        }
    }

    private static Dictionary<string, int> CountByName(IEnumerable<string> values)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value)))
            counts[value] = counts.GetValueOrDefault(value, 0) + 1;
        return counts;
    }

    private async void OnShuffleClicked(object? sender, RoutedEventArgs e)
    {
        _shuffle = !_shuffle;
        ShuffleButton.Opacity = _shuffle ? 1.0 : 0.45;
        ShuffleButton.Content = _shuffle ? "Shuffle on" : "Shuffle";
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
        StatusText.Text = "Importing library...";

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
            _audio.Seek(TimeSpan.FromSeconds(seconds));
        }

        _isSeeking = false;
    }

    private void UpdatePlaybackUi()
    {
        if (!_isSeeking && _audio.Duration.TotalSeconds > 0)
            ProgressSlider.Value = _audio.Position.TotalSeconds / _audio.Duration.TotalSeconds * 100.0;

        TimeText.Text = $"{Format(_audio.Position)} / {Format(_audio.Duration)}";
        if (_currentIndex >= 0)
            PlayPauseButton.Content = _audio.IsPlaying ? "II" : ">";
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";

    private void ShuffleFilteredTracks()
    {
        for (var i = _filteredTracks.Count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (_filteredTracks[i], _filteredTracks[j]) = (_filteredTracks[j], _filteredTracks[i]);
        }
    }
}
