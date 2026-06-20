using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    public SettingsOverlay()
    {
        InitializeComponent();
        SearchBox.TextChanged += (_, _) => RebuildMappingRows();
        ModelGenreBox.SelectionChanged += (_, _) =>
        {
            RebuildMappingRows();
            UpdateSummary();
        };
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
        SelectPage(value == "library" ? SettingsPage.Library : SettingsPage.GenreMappings);
    }

    private void SelectPage(SettingsPage page)
    {
        _selectedPage = page;
        var isMappingsPage = page == SettingsPage.GenreMappings;
        GenreMappingsPage.IsVisible = isMappingsPage;
        LibraryPage.IsVisible = !isMappingsPage;
        GenreMappingsNavButton.IsChecked = isMappingsPage;
        LibraryNavButton.IsChecked = !isMappingsPage;

        PageTitleText.Text = isMappingsPage ? "Genre mappings" : "Library";
        PageDescriptionText.Text = isMappingsPage
            ? "Connect model subgenres with your own genres. Unassigned labels remain visible as raw model output."
            : "Where this installation keeps the local music library and its database.";
        SummaryText.Text = isMappingsPage ? BuildSummaryText() : "";
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
    private enum SettingsPage { GenreMappings, Library }
}
