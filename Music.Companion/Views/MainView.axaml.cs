using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    private int _currentIndex = -1;
    private bool _isSeeking;

    public MainView()
    {
        InitializeComponent();

        SearchBox.TextChanged += (_, _) => ApplyFilter();
        RatingFilter.SelectionChanged += (_, _) => ApplyFilter();
        GenreFilter.SelectionChanged += (_, _) => ApplyFilter();
        StyleFilter.SelectionChanged += (_, _) => ApplyFilter();

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
        SetFilterItems(RatingFilter, "All ratings", _loadedLibrary.Library.Ratings);
        SetFilterItems(GenreFilter, "All genres", _loadedLibrary.Library.Genres);
        SetFilterItems(StyleFilter, "All styles", _loadedLibrary.Library.Styles);
    }

    private static void SetFilterItems(ComboBox combo, string allText, IEnumerable<string> values)
    {
        combo.ItemsSource = new[] { allText }.Concat(values).ToList();
        combo.SelectedIndex = 0;
    }

    private void ApplyFilter()
    {
        _filteredTracks = PortableTrackFilter.Apply(
            _loadedLibrary.Library.Tracks,
            SearchBox.Text,
            SelectedFilter(RatingFilter),
            SelectedFilter(GenreFilter),
            SelectedFilter(StyleFilter));

        TrackList.ItemsSource = _filteredTracks;
        if (_currentIndex >= _filteredTracks.Count)
            _currentIndex = -1;
    }

    private static string? SelectedFilter(ComboBox combo) =>
        combo.SelectedIndex > 0 ? combo.SelectedItem as string : null;

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
        PlayPauseButton.Content = "Pause";
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
            PlayPauseButton.Content = "Play";
        }
        else
        {
            _audio.Resume();
            PlayPauseButton.Content = "Pause";
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
            PlayPauseButton.Content = "Play";
            return;
        }

        _ = PlayTrackAtAsync(next);
    }

    private void OnReloadClicked(object? sender, RoutedEventArgs e)
    {
        _ = LoadLibraryAsync();
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
            PlayPauseButton.Content = _audio.IsPlaying ? "Pause" : "Play";
    }

    private static string Format(TimeSpan time) =>
        $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}";
}
