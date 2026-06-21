using System;
using System.Collections.Generic;
using System.Globalization;
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
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private Dictionary<int, GenreMapping> _mappingsBySubgenreId = [];
    private List<TagCategory> _tagCategories = [];
    private List<Tag> _tags = [];
    private List<TagSignalSource> _tagSignalSources = [];
    private List<TagRule> _tagRules = [];
    private bool _isLoading;
    private bool _updatingTagCategory;
    private MappingFilter _mappingFilter = MappingFilter.All;
    private SettingsPage _selectedPage;

    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? TrackCalibrationRequested;
    public event Action? LibraryMetadataChanged;

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
        _distinctionsBySubgenreId = MusicLibraryService.Current.GetModelSubgenreDistinctions()
            .GroupBy(item => item.ModelSubgenreId)
            .ToDictionary(group => group.Key, group => group.ToList());
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
        ReloadTagManagement();
        ReloadTagRules();
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
               || (subgenre.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
               || (subgenre.ClassificationHint?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
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
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,220"),
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
            RowSpacing = 5,
            ColumnSpacing = 16
        };
        grid.Children.Add(new TextBlock
        {
            Text = $"{modelGenreName}  ·  {subgenre.Name}",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        });
        var bpm = new TextBlock
        {
            Text = BpmText(subgenre), FontSize = 10.5, Opacity = 0.68,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(bpm, 1);
        grid.Children.Add(bpm);

        if (!string.IsNullOrWhiteSpace(subgenre.Description))
        {
            var description = new TextBlock
            {
                Text = subgenre.Description, FontSize = 10.5, Opacity = 0.74, TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(description, 1);
            grid.Children.Add(description);
        }

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
        Grid.SetColumn(choiceBox, 1);
        Grid.SetRow(choiceBox, 2);
        choiceBox.VerticalAlignment = VerticalAlignment.Bottom;
        grid.Children.Add(choiceBox);

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(subgenre.ClassificationHint))
            details.Add($"Classify when: {subgenre.ClassificationHint}");
        if (_distinctionsBySubgenreId.TryGetValue(subgenre.Id, out var distinctions))
            details.Add("Distinguish from: " + string.Join(" · ", distinctions.Select(item =>
                $"{item.ModelGenreName} → {item.ModelSubgenreName} — {item.Difference}")));
        if (details.Count > 0)
        {
            var detailsText = new TextBlock
            {
                Text = string.Join("\n", details),
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.Parse("#83BBD9")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Avalonia.Thickness(0, 1, 0, 0)
            };
            Grid.SetRow(detailsText, 2);
            grid.Children.Add(detailsText);
        }

        row.Child = grid;
        return row;
    }

    private static string BpmText(ModelSubgenre subgenre) => subgenre.BpmMin is not null && subgenre.BpmMax is not null
        ? $"Typical BPM · {subgenre.BpmMin}–{subgenre.BpmMax}"
        : subgenre.BpmMin is not null ? $"Typical BPM · from {subgenre.BpmMin}"
        : subgenre.BpmMax is not null ? $"Typical BPM · up to {subgenre.BpmMax}"
        : string.Empty;

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
        LibraryMetadataChanged?.Invoke();
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
            "tags" => SettingsPage.Tags,
            "tag_rules" => SettingsPage.TagRules,
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
        TagsPage.IsVisible = page == SettingsPage.Tags;
        TagRulesPage.IsVisible = page == SettingsPage.TagRules;
        GenreMappingsNavButton.IsChecked = isMappingsPage;
        LibraryNavButton.IsChecked = isLibraryPage;
        AnalysisCalibrationNavButton.IsChecked = page == SettingsPage.AnalysisCalibration;
        GenresNavButton.IsChecked = page == SettingsPage.Genres;
        TagsNavButton.IsChecked = page == SettingsPage.Tags;
        TagRulesNavButton.IsChecked = page == SettingsPage.TagRules;

        PageTitleText.Text = page switch
        {
            SettingsPage.Library => "Library",
            SettingsPage.AnalysisCalibration => "Analysis calibration",
            SettingsPage.Genres => "Your genres",
            SettingsPage.Tags => "Tags",
            SettingsPage.TagRules => "Tag rules",
            _ => "Genre mappings"
        };
        PageDescriptionText.Text = isMappingsPage
            ? "Connect model subgenres with your own genres. Unassigned labels remain visible as raw model output."
            : isLibraryPage
                ? "Where this installation keeps the local music library and its database."
                : page == SettingsPage.Genres
                    ? "Create and maintain the genres used by your library. Genres with existing tracks or mappings stay protected."
                    : page == SettingsPage.Tags
                        ? "Maintain your curated labels. Tags can describe mood, themes, situations or workflow states without turning them into genres."
                        : page == SettingsPage.TagRules
                            ? "Turn model signals into reviewable tag suggestions. Rules never assign tags automatically in this first version."
                        : "Compare current system interpretations before turning them into filters.";
        SummaryText.Text = isMappingsPage ? BuildSummaryText() : "";
        if (page == SettingsPage.AnalysisCalibration) RebuildCalibrationRows();
        if (page == SettingsPage.Genres) RebuildGenreRows();
        if (page == SettingsPage.Tags) ReloadTagManagement(SelectedTagCategoryId());
        if (page == SettingsPage.TagRules) ReloadTagRules();
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
                try
                {
                    MusicLibraryService.Current.RenameGenre(genre.Id, nameBox.Text ?? genre.Name);
                    ToastRequested?.Invoke("Genre updated.");
                    RebuildGenreRows();
                    LibraryMetadataChanged?.Invoke();
                }
                catch (Exception exception) { ToastRequested?.Invoke($"Could not update genre: {exception.Message}"); }
            };
            remove.Click += (_, _) =>
            {
                var error = MusicLibraryService.Current.DeleteGenreIfUnused(genre.Id);
                ToastRequested?.Invoke(error ?? "Genre deleted.");
                RebuildGenreRows();
                if (error is null) LibraryMetadataChanged?.Invoke();
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
        try
        {
            MusicLibraryService.Current.AddGenre(name);
            NewGenreBox.Text = string.Empty;
            RebuildGenreRows();
            ToastRequested?.Invoke("Genre added.");
            LibraryMetadataChanged?.Invoke();
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add genre: {exception.Message}"); }
    }

    private void ReloadTagManagement(int? selectedCategoryId = null)
    {
        if (!IsInitialized) return;
        _tagCategories = MusicLibraryService.Current.GetTagCategories();
        _tags = MusicLibraryService.Current.GetTags();

        var choices = _tagCategories
            .Select(category => new TagCategoryChoice(category.Id, category.Name))
            .ToList();

        _updatingTagCategory = true;
        TagCategoryBox.ItemsSource = choices;
        if (choices.Count == 0)
        {
            TagCategoryBox.SelectedIndex = -1;
            RenameTagCategoryBox.Text = string.Empty;
        }
        else
        {
            TagCategoryBox.SelectedItem = choices.FirstOrDefault(choice => choice.Id == selectedCategoryId)
                                          ?? choices[0];
        }
        _updatingTagCategory = false;

        UpdateSelectedTagCategoryFields();
        RebuildTagRows();
    }

    private void ReloadTagRules()
    {
        if (!IsInitialized) return;
        _tags = MusicLibraryService.Current.GetTags();
        _tagSignalSources = MusicLibraryService.Current.GetTagSignalSources();
        _tagRules = MusicLibraryService.Current.GetTagRules();

        TagRuleTagBox.ItemsSource = _tags
            .Select(tag => new TagRuleTagChoice(tag.Id, tag.CategoryName, tag.Name, tag.CategoryColor))
            .ToList();
        TagRuleModelBox.ItemsSource = _tagSignalSources
            .Select(source => source.ModelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(TagRuleModelDisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(model => new TagRuleModelChoice(model))
            .ToList();

        if (TagRuleTagBox.SelectedIndex < 0 && _tags.Count > 0)
            TagRuleTagBox.SelectedIndex = 0;
        if (TagRuleModelBox.SelectedIndex < 0 && _tagSignalSources.Count > 0)
            TagRuleModelBox.SelectedIndex = 0;
        RebuildTagRuleSignalChoices();

        RebuildTagRuleRows();
    }

    private void OnTagRuleModelSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        RebuildTagRuleSignalChoices();

    private void RebuildTagRuleSignalChoices()
    {
        if (TagRuleModelBox.SelectedItem is not TagRuleModelChoice model)
        {
            TagRuleSignalBox.ItemsSource = Array.Empty<TagRuleSignalChoice>();
            return;
        }

        TagRuleSignalBox.ItemsSource = _tagSignalSources
            .Where(source => string.Equals(source.ModelName, model.ModelName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(source => source.SignalKey, StringComparer.OrdinalIgnoreCase)
            .Select(source => new TagRuleSignalChoice(source.ModelName, source.SignalKey, source.Description))
            .ToList();
        if (TagRuleSignalBox.SelectedIndex < 0)
            TagRuleSignalBox.SelectedIndex = 0;
    }

    private void RebuildTagRuleRows()
    {
        if (!IsInitialized) return;
        TagRuleRows.Children.Clear();
        if (_tagSignalSources.Count == 0)
        {
            TagRuleRows.Children.Add(new TextBlock
            {
                Text = "Analyze at least one track first. Its available model signals will appear here as possible rule sources.",
                Opacity = .52, TextWrapping = TextWrapping.Wrap, Margin = new Avalonia.Thickness(0, 18, 0, 0)
            });
            return;
        }

        foreach (var rule in _tagRules)
        {
            var accent = CategoryBrush(rule.CategoryColor);
            var row = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#111419")),
                BorderBrush = new SolidColorBrush(Color.Parse("#26313A")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(10, 7)
            };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("180,*,70,Auto,Auto"), ColumnSpacing = 10 };
            var tag = new TextBlock
            {
                Text = $"{rule.CategoryName} · {rule.TagName}",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = accent,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var source = new TextBlock
            {
                Text = $"{TagRuleModelDisplayName(rule.SourceType)} · {rule.SourceKey}",
                FontSize = 10.5,
                Opacity = .7,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var threshold = new TextBlock
            {
                Text = $"≥ {rule.Threshold:0.##}",
                FontSize = 10.5,
                Foreground = accent,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var enabled = new ToggleSwitch
            {
                IsChecked = rule.Enabled,
                OnContent = "On",
                OffContent = "Off",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            enabled.IsCheckedChanged += (_, _) =>
            {
                MusicLibraryService.Current.SetTagRuleEnabled(rule.Id, enabled.IsChecked == true);
                MusicLibraryService.Current.RefreshAllTagSuggestions();
            };
            var remove = new Button
            {
                Content = "Delete",
                FontSize = 10,
                Padding = new Avalonia.Thickness(10, 4),
                Opacity = .75
            };
            remove.Click += (_, _) =>
            {
                MusicLibraryService.Current.DeleteTagRule(rule.Id);
                MusicLibraryService.Current.RefreshAllTagSuggestions();
                ReloadTagRules();
                ToastRequested?.Invoke("Tag rule deleted.");
            };
            Grid.SetColumn(source, 1);
            Grid.SetColumn(threshold, 2);
            Grid.SetColumn(enabled, 3);
            Grid.SetColumn(remove, 4);
            grid.Children.Add(tag);
            grid.Children.Add(source);
            grid.Children.Add(threshold);
            grid.Children.Add(enabled);
            grid.Children.Add(remove);
            row.Child = grid;
            TagRuleRows.Children.Add(row);
        }

        if (_tagRules.Count == 0)
            TagRuleRows.Children.Add(new TextBlock { Text = "No rules yet. Select one of your tags, choose a model signal and set its minimum score.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
    }

    private void OnAddTagRuleClicked(object? sender, RoutedEventArgs e)
    {
        if (TagRuleTagBox.SelectedItem is not TagRuleTagChoice tag
            || TagRuleSignalBox.SelectedItem is not TagRuleSignalChoice source)
            return;

        if (!double.TryParse(TagRuleThresholdBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var threshold)
            || threshold < 0 || threshold > 1)
        {
            ToastRequested?.Invoke("Threshold must be a number between 0 and 1.");
            return;
        }

        try
        {
            MusicLibraryService.Current.AddTagRule(tag.Id, source.ModelName, source.SignalKey, threshold);
            MusicLibraryService.Current.RefreshAllTagSuggestions();
            ReloadTagRules();
            ToastRequested?.Invoke($"Suggestion rule added for {tag.Name}.");
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add tag rule: {exception.Message}"); }
    }

    private int? SelectedTagCategoryId() => (TagCategoryBox.SelectedItem as TagCategoryChoice)?.Id;

    private void OnTagCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingTagCategory) return;
        UpdateSelectedTagCategoryFields();
        RebuildTagRows();
    }

    private void UpdateSelectedTagCategoryFields()
    {
        var categoryId = SelectedTagCategoryId();
        var category = _tagCategories.FirstOrDefault(item => item.Id == categoryId);
        RenameTagCategoryBox.Text = category?.Name ?? string.Empty;
    }

    private void RebuildTagRows()
    {
        if (!IsInitialized) return;
        TagRows.Children.Clear();
        var categoryId = SelectedTagCategoryId();
        if (categoryId is null)
        {
            TagRows.Children.Add(new TextBlock { Text = "Create a tag category first.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
            return;
        }

        var tags = _tags
            .Where(tag => tag.CategoryId == categoryId.Value)
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var tag in tags)
        {
            var row = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#111419")),
                BorderBrush = new SolidColorBrush(Color.Parse("#26313A")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(9, 7)
            };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("190,*,Auto,Auto"), ColumnSpacing = 8 };
            var nameBox = new TextBox { Text = tag.Name, Height = 32, FontSize = 11 };
            var descriptionBox = new TextBox { Text = tag.Description ?? string.Empty, Height = 32, FontSize = 11, Watermark = "Description / usage hint…" };
            var save = new Button { Content = "Save", Padding = new Avalonia.Thickness(10, 4), FontSize = 10 };
            var remove = new Button { Content = "Delete", Padding = new Avalonia.Thickness(10, 4), FontSize = 10, Opacity = .75 };
            save.Click += (_, _) =>
            {
                try
                {
                    MusicLibraryService.Current.RenameTag(tag.Id, nameBox.Text ?? tag.Name, descriptionBox.Text);
                    ToastRequested?.Invoke("Tag updated.");
                    ReloadTagManagement(categoryId);
                    LibraryMetadataChanged?.Invoke();
                }
                catch (Exception exception) { ToastRequested?.Invoke($"Could not update tag: {exception.Message}"); }
            };
            remove.Click += (_, _) =>
            {
                var error = MusicLibraryService.Current.DeleteTagIfUnused(tag.Id);
                ToastRequested?.Invoke(error ?? "Tag deleted.");
                ReloadTagManagement(categoryId);
                if (error is null) LibraryMetadataChanged?.Invoke();
            };
            Grid.SetColumn(descriptionBox, 1);
            Grid.SetColumn(save, 2);
            Grid.SetColumn(remove, 3);
            grid.Children.Add(nameBox);
            grid.Children.Add(descriptionBox);
            grid.Children.Add(save);
            grid.Children.Add(remove);
            row.Child = grid;
            TagRows.Children.Add(row);
        }

        if (tags.Count == 0)
            TagRows.Children.Add(new TextBlock { Text = "No tags in this category yet.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
    }

    private void OnAddTagCategoryClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewTagCategoryBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        try
        {
            MusicLibraryService.Current.AddTagCategory(name);
            NewTagCategoryBox.Text = string.Empty;
            ReloadTagManagement();
            ToastRequested?.Invoke("Tag category added.");
            LibraryMetadataChanged?.Invoke();
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add tag category: {exception.Message}"); }
    }

    private void OnSaveTagCategoryClicked(object? sender, RoutedEventArgs e)
    {
        if (SelectedTagCategoryId() is not int categoryId) return;
        var name = RenameTagCategoryBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        try
        {
            MusicLibraryService.Current.RenameTagCategory(categoryId, name);
            ReloadTagManagement(categoryId);
            ToastRequested?.Invoke("Tag category updated.");
            LibraryMetadataChanged?.Invoke();
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not update tag category: {exception.Message}"); }
    }

    private void OnDeleteTagCategoryClicked(object? sender, RoutedEventArgs e)
    {
        if (SelectedTagCategoryId() is not int categoryId) return;
        var error = MusicLibraryService.Current.DeleteTagCategoryIfUnused(categoryId);
        ToastRequested?.Invoke(error ?? "Tag category deleted.");
        ReloadTagManagement();
        if (error is null) LibraryMetadataChanged?.Invoke();
    }

    private void OnAddTagClicked(object? sender, RoutedEventArgs e)
    {
        if (SelectedTagCategoryId() is not int categoryId) return;
        var name = NewTagBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        try
        {
            MusicLibraryService.Current.AddTag(categoryId, name, NewTagDescriptionBox.Text);
            NewTagBox.Text = string.Empty;
            NewTagDescriptionBox.Text = string.Empty;
            ReloadTagManagement(categoryId);
            ToastRequested?.Invoke("Tag added.");
            LibraryMetadataChanged?.Invoke();
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add tag: {exception.Message}"); }
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

    private static IBrush CategoryBrush(string? color)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? "#65BCEB" : color)); }
        catch { return new SolidColorBrush(Color.Parse("#65BCEB")); }
    }

    private static string TagRuleModelDisplayName(string modelName) => modelName switch
    {
        "mood happy" => "Mood happy",
        "mood sad" => "Mood sad",
        "mood relaxed" => "Mood relaxed",
        "mood aggressive" => "Mood aggressive",
        "mood party" => "Mood party",
        "mtg_jamendo_moodtheme" => "Jamendo mood/theme",
        "moods mirex" => "MIREX mood clusters",
        "genre electronic" => "Electronic character",
        "danceability classifier" => "Danceability",
        "voice/instrumental classifiers" => "Voice / instrumental",
        _ => modelName
    };

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;

    private sealed record MappingChoice(int? GenreId, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record ModelGenreChoice(int? Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record TagCategoryChoice(int Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record TagRuleTagChoice(int Id, string CategoryName, string Name, string? CategoryColor)
    {
        public override string ToString() => $"{CategoryName} · {Name}";
    }

    private sealed record TagRuleModelChoice(string ModelName)
    {
        public override string ToString() => TagRuleModelDisplayName(ModelName);
    }

    private sealed record TagRuleSignalChoice(string ModelName, string SignalKey, string Description)
    {
        public override string ToString() => SignalKey;
    }

    private enum MappingFilter { All, Mapped, Unmapped }
    private sealed record CalibrationRow(MusicTrack Track, List<DerivedTrackAttribute> Attributes, IReadOnlyList<ExperimentalAnalysisModel> Signals)
    {
        public string Value(string key) => Attributes.FirstOrDefault(attribute => attribute.Key == key)?.EffectiveValue ?? "—";
        public string Evidence => string.Join(" · ", Signals.SelectMany(model => model.Values.Select(value => (model.Model, value)))
            .OrderByDescending(item => item.value.Score).Take(3).Select(item => $"{item.Model}: {item.value.Label} {item.value.Score:0.##}"));
    }

    private enum SettingsPage { GenreMappings, Library, AnalysisCalibration, Genres, Tags, TagRules }
}
