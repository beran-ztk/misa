using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class SettingsOverlay : UserControl
{
    private readonly List<MappingChoice> _mappingChoices = [];
    private List<ModelGenre> _modelGenres = [];
    private List<ModelSubgenre> _modelSubgenres = [];
    private Dictionary<int, GenreMapping> _mappingsBySubgenreId = [];
    private bool _isLoading;
    private MappingFilter _mappingFilter = MappingFilter.All;
    private SettingsPage _selectedPage;

    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? TrackCalibrationRequested;

    public SettingsOverlay()
    {
        InitializeComponent();
        SearchBox.TextChanged += (_, _) => RebuildMappingRows();
        ModelGenreBox.SelectionChanged += (_, _) =>
        {
            RebuildMappingRows();
            UpdateSummary();
        };
        CalibrationSortBox.ItemsSource = new[] { "Recently added", "Tone", "Energy", "Intensity" };
        CalibrationSortBox.SelectedIndex = 0;
        CalibrationSortBox.SelectionChanged += (_, _) => RebuildCalibrationRows();
    }

    public void Open()
    {
        _isLoading = true;
        DatabasePathText.Text = Values.DbPath;
        TracksPathText.Text = Values.TracksDirectory;
        var genres = MusicLibraryService.Current.GetGenres();
        _mappingChoices.Clear();
        _mappingChoices.Add(new MappingChoice(null, "Not assigned"));
        _mappingChoices.AddRange(genres.Select(genre => new MappingChoice(genre.Id, genre.Name)));

        _modelGenres = MusicLibraryService.Current.GetModelGenres();
        _modelSubgenres = MusicLibraryService.Current.GetModelSubgenres();
        _mappingsBySubgenreId = MusicLibraryService.Current.GetGenreMappings()
            .ToDictionary(mapping => mapping.ModelSubgenreId);

        ModelGenreBox.ItemsSource = new[] { new ModelGenreChoice(null, "All model genres") }
            .Concat(_modelGenres.Select(genre => new ModelGenreChoice(genre.Id, genre.Name)))
            .ToList();
        ModelGenreBox.SelectedIndex = 0;
        SearchBox.Text = string.Empty;
        SetMappingFilter(MappingFilter.All);
        _isLoading = false;

        SelectPage(SettingsPage.GenreMappings);
        UpdateSummary();
        RebuildMappingRows();
        RebuildCalibrationRows();
        RebuildGenreRows();
        IsVisible = true;
    }

    private void RebuildMappingRows()
    {
        if (_isLoading) return;

        MappingRows.Children.Clear();
        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var selectedModelGenreId = (ModelGenreBox.SelectedItem as ModelGenreChoice)?.Id;
        var modelGenreNames = _modelGenres.ToDictionary(genre => genre.Id, genre => genre.Name);

        var rows = _modelSubgenres
            .Where(subgenre => selectedModelGenreId is null || subgenre.ModelGenreId == selectedModelGenreId)
            .Where(MatchesMappingFilter)
            .Where(subgenre => MatchesSearch(subgenre, modelGenreNames[subgenre.ModelGenreId], search))
            .OrderBy(subgenre => modelGenreNames[subgenre.ModelGenreId])
            .ThenBy(subgenre => subgenre.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var subgenre in rows)
            MappingRows.Children.Add(CreateMappingRow(subgenre, modelGenreNames[subgenre.ModelGenreId]));

        if (rows.Count == 0)
        {
            MappingRows.Children.Add(new TextBlock
            {
                Text = "No model subgenres match the current filter.",
                Opacity = 0.52,
                Margin = new Avalonia.Thickness(0, 18, 0, 0)
            });
        }
    }

    private bool MatchesSearch(ModelSubgenre subgenre, string modelGenreName, string search)
    {
        if (search.Length == 0) return true;
        var mappedGenreName = _mappingsBySubgenreId.TryGetValue(subgenre.Id, out var mapping)
            ? mapping.GenreName
            : string.Empty;
        return modelGenreName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || subgenre.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || mappedGenreName.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private Control CreateMappingRow(ModelSubgenre subgenre, string modelGenreName)
    {
        var row = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#111419")),
            BorderBrush = new SolidColorBrush(Color.Parse("#26313A")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(5),
            Padding = new Avalonia.Thickness(10, 7)
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("150,*,220") };
        grid.Children.Add(new TextBlock
        {
            Text = modelGenreName,
            FontSize = 11,
            Opacity = 0.58,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        });
        var subgenreText = new TextBlock
        {
            Text = subgenre.Name,
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(subgenreText, 1);
        grid.Children.Add(subgenreText);

        var choiceBox = new ComboBox
        {
            ItemsSource = _mappingChoices,
            Tag = subgenre.Id,
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var assignedGenreId = _mappingsBySubgenreId.TryGetValue(subgenre.Id, out var mapping)
            ? mapping.GenreId
            : (int?)null;
        choiceBox.SelectedItem = _mappingChoices.Single(choice => choice.GenreId == assignedGenreId);
        choiceBox.SelectionChanged += OnMappingSelectionChanged;
        Grid.SetColumn(choiceBox, 2);
        grid.Children.Add(choiceBox);

        row.Child = grid;
        return row;
    }

    private void OnMappingSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || sender is not ComboBox { Tag: int modelSubgenreId, SelectedItem: MappingChoice choice })
            return;

        if (choice.GenreId is int genreId)
        {
            MusicLibraryService.Current.SetGenreMapping(genreId, modelSubgenreId);
            var genreName = choice.Name;
            _mappingsBySubgenreId[modelSubgenreId] = new GenreMapping(
                0, genreId, genreName, modelSubgenreId, 0, string.Empty);
            ToastRequested?.Invoke($"Mapped {SubgenreName(modelSubgenreId)} to {genreName}");
        }
        else
        {
            MusicLibraryService.Current.RemoveGenreMapping(modelSubgenreId);
            _mappingsBySubgenreId.Remove(modelSubgenreId);
            ToastRequested?.Invoke($"Removed mapping for {SubgenreName(modelSubgenreId)}");
        }

        UpdateSummary();
        if (_mappingFilter != MappingFilter.All)
            RebuildMappingRows();
    }

    private void OnMappingFilterClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string value }) return;
        SetMappingFilter(value switch
        {
            "mapped" => MappingFilter.Mapped,
            "unmapped" => MappingFilter.Unmapped,
            _ => MappingFilter.All
        });
        RebuildMappingRows();
    }

    private void OnSettingsNavigationClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string value }) return;
        SelectPage(value switch
        {
            "library" => SettingsPage.Library,
            "calibration" => SettingsPage.AnalysisCalibration,
            "genres" => SettingsPage.Genres,
            _ => SettingsPage.GenreMappings
        });
    }

    private void SelectPage(SettingsPage page)
    {
        _selectedPage = page;
        var isMappingsPage = page == SettingsPage.GenreMappings;
        var isLibraryPage = page == SettingsPage.Library;
        GenreMappingsPage.IsVisible = isMappingsPage;
        LibraryPage.IsVisible = isLibraryPage;
        AnalysisCalibrationPage.IsVisible = page == SettingsPage.AnalysisCalibration;
        GenresPage.IsVisible = page == SettingsPage.Genres;
        GenreMappingsNavButton.IsChecked = isMappingsPage;
        LibraryNavButton.IsChecked = isLibraryPage;
        AnalysisCalibrationNavButton.IsChecked = page == SettingsPage.AnalysisCalibration;
        GenresNavButton.IsChecked = page == SettingsPage.Genres;

        PageTitleText.Text = page switch
        {
            SettingsPage.Library => "Library",
            SettingsPage.AnalysisCalibration => "Analysis calibration",
            SettingsPage.Genres => "Your genres",
            _ => "Genre mappings"
        };
        PageDescriptionText.Text = isMappingsPage
            ? "Connect model subgenres with your own genres. Unassigned labels remain visible as raw model output."
            : isLibraryPage
                ? "Where this installation keeps the local music library and its database."
                : page == SettingsPage.Genres
                    ? "Create and maintain the genres used by your library. Genres with existing tracks or mappings stay protected."
                    : "Compare current system interpretations before turning them into filters.";
        SummaryText.Text = isMappingsPage ? BuildSummaryText() : "";
        if (page == SettingsPage.AnalysisCalibration) RebuildCalibrationRows();
        if (page == SettingsPage.Genres) RebuildGenreRows();
    }

    private void RebuildGenreRows()
    {
        if (!IsInitialized) return;
        GenreRows.Children.Clear();
        foreach (var genre in MusicLibraryService.Current.GetGenres())
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            var nameBox = new TextBox { Text = genre.Name, Height = 32 };
            var save = new Button { Content = "Save", Padding = new Avalonia.Thickness(10, 4), Margin = new Avalonia.Thickness(8, 0, 0, 0), FontSize = 10 };
            var remove = new Button { Content = "Delete", Padding = new Avalonia.Thickness(10, 4), Margin = new Avalonia.Thickness(6, 0, 0, 0), FontSize = 10 };
            save.Click += (_, _) =>
            {
                try { MusicLibraryService.Current.RenameGenre(genre.Id, nameBox.Text ?? genre.Name); ToastRequested?.Invoke("Genre updated."); RebuildGenreRows(); }
                catch (Exception exception) { ToastRequested?.Invoke($"Could not update genre: {exception.Message}"); }
            };
            remove.Click += (_, _) =>
            {
                var error = MusicLibraryService.Current.DeleteGenreIfUnused(genre.Id);
                ToastRequested?.Invoke(error ?? "Genre deleted.");
                RebuildGenreRows();
            };
            Grid.SetColumn(save, 1); Grid.SetColumn(remove, 2);
            row.Children.Add(nameBox); row.Children.Add(save); row.Children.Add(remove);
            GenreRows.Children.Add(row);
        }
    }

    private void OnAddGenreClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewGenreBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        try { MusicLibraryService.Current.AddGenre(name); NewGenreBox.Text = string.Empty; RebuildGenreRows(); ToastRequested?.Invoke("Genre added."); }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add genre: {exception.Message}"); }
    }

    private void RebuildCalibrationRows()
    {
        if (!IsInitialized) return;
        CalibrationRows.Children.Clear();
        var rows = MusicLibraryService.Current.GetTracks()
            .Select(track => new CalibrationRow(track, MusicLibraryService.Current.GetTrackDerivedAttributes(track.Id),
                MusicLibraryService.Current.GetExperimentalAnalysis(track.Id)))
            .Where(row => row.Attributes.Count > 0)
            .ToList();
        rows = (CalibrationSortBox.SelectedItem as string) switch
        {
            "Tone" => rows.OrderBy(row => row.Value("emotional_tone")).ThenBy(row => row.Track.Title).ToList(),
            "Energy" => rows.OrderBy(row => row.Value("energy_context")).ThenBy(row => row.Track.Title).ToList(),
            "Intensity" => rows.OrderBy(row => row.Value("intensity")).ThenBy(row => row.Track.Title).ToList(),
            _ => rows.OrderByDescending(row => row.Track.DownloadedAt).ToList()
        };
        foreach (var row in rows) CalibrationRows.Children.Add(CreateCalibrationRow(row));
        if (rows.Count == 0) CalibrationRows.Children.Add(new TextBlock { Text = "No analyzed tracks are available yet.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
    }

    private Control CreateCalibrationRow(CalibrationRow row)
    {
        var button = new Button { Background = new SolidColorBrush(Color.Parse("#111419")), BorderBrush = new SolidColorBrush(Color.Parse("#26313A")), BorderThickness = new Avalonia.Thickness(1), Padding = new Avalonia.Thickness(11, 9), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,100,100,100,2*") };
        grid.Children.Add(new TextBlock { Text = row.Track.Title, FontSize = 12, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
        AddCell(row.Value("emotional_tone"), 1); AddCell(row.Value("energy_context"), 2); AddCell(row.Value("intensity"), 3);
        var evidence = new TextBlock { Text = row.Evidence, FontSize = 10.5, Opacity = .64, TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(evidence, 4); grid.Children.Add(evidence);
        button.Content = grid;
        button.Click += (_, _) => TrackCalibrationRequested?.Invoke(row.Track);
        return button;

        void AddCell(string value, int column)
        {
            var text = new TextBlock { Text = value, FontSize = 11, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(text, column); grid.Children.Add(text);
        }
    }

    private void SetMappingFilter(MappingFilter filter)
    {
        _mappingFilter = filter;
        AllFilterButton.IsChecked = filter == MappingFilter.All;
        MappedFilterButton.IsChecked = filter == MappingFilter.Mapped;
        UnmappedFilterButton.IsChecked = filter == MappingFilter.Unmapped;
    }

    private bool MatchesMappingFilter(ModelSubgenre subgenre) => _mappingFilter switch
    {
        MappingFilter.Mapped => _mappingsBySubgenreId.ContainsKey(subgenre.Id),
        MappingFilter.Unmapped => !_mappingsBySubgenreId.ContainsKey(subgenre.Id),
        _ => true
    };

    private void UpdateSummary()
    {
        if (_selectedPage != SettingsPage.GenreMappings) return;
        SummaryText.Text = BuildSummaryText();
    }

    private string BuildSummaryText()
    {
        var selected = ModelGenreBox.SelectedItem as ModelGenreChoice;
        var relevantSubgenres = selected?.Id is int modelGenreId
            ? _modelSubgenres.Where(subgenre => subgenre.ModelGenreId == modelGenreId).ToList()
            : _modelSubgenres;
        var mappedCount = relevantSubgenres.Count(subgenre => _mappingsBySubgenreId.ContainsKey(subgenre.Id));
        var scope = selected?.Id is null ? "All model genres" : selected!.Name;
        return $"{scope}: {mappedCount} of {relevantSubgenres.Count} model subgenres mapped.";
    }

    private string SubgenreName(int modelSubgenreId) => _modelSubgenres
        .FirstOrDefault(subgenre => subgenre.Id == modelSubgenreId)?.Name ?? "model subgenre";

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;

    private sealed record MappingChoice(int? GenreId, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record ModelGenreChoice(int? Id, string Name)
    {
        public override string ToString() => Name;
    }

    private enum MappingFilter { All, Mapped, Unmapped }
    private sealed record CalibrationRow(MusicTrack Track, List<DerivedTrackAttribute> Attributes, IReadOnlyList<ExperimentalAnalysisModel> Signals)
    {
        public string Value(string key) => Attributes.FirstOrDefault(attribute => attribute.Key == key)?.EffectiveValue ?? "—";
        public string Evidence => string.Join(" · ", Signals.SelectMany(model => model.Values.Select(value => (model.Model, value)))
            .OrderByDescending(item => item.value.Score).Take(3).Select(item => $"{item.Model}: {item.value.Label} {item.value.Score:0.##}"));
    }

    private enum SettingsPage { GenreMappings, Library, AnalysisCalibration, Genres }
}
