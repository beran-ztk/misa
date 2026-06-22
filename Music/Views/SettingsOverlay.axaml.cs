using System;
using System.Collections.Generic;
using System.Globalization;
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
    private List<ModelGenre> _modelGenres = [];
    private List<ModelSubgenre> _modelSubgenres = [];
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private List<TagCategory> _tagCategories = [];
    private List<Tag> _tags = [];
    private List<TagSignalSource> _tagSignalSources = [];
    private List<TagRuleGroup> _tagRuleGroups = [];
    private bool _isLoading;
    private bool _updatingTagCategory;
    private bool _genreVocabularyLoaded;
    private readonly Dictionary<int, GenreVocabularyRowState> _genreVocabularyRowsById = [];
    private TextBlock? _emptyGenreVocabularyText;
    private SettingsPage _selectedPage;

    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? TrackCalibrationRequested;
    public event Action? LibraryMetadataChanged;
    public event Action? ExportRequested;

    public SettingsOverlay()
    {
        InitializeComponent();
        SearchBox.TextChanged += (_, _) => RebuildGenreVocabularyRows();
        ModelGenreBox.SelectionChanged += (_, _) =>
        {
            RebuildGenreVocabularyRows();
            UpdateSummary();
        };
        CalibrationSortBox.ItemsSource = new[] { "Recently added", "Tone", "Energy", "Intensity" };
        CalibrationSortBox.SelectedIndex = 0;
        CalibrationSortBox.SelectionChanged += (_, _) => RebuildCalibrationRows();
    }

    public void Open()
    {
        DatabasePathText.Text = Values.DbPath;
        TracksPathText.Text = Values.TracksDirectory;
        SelectPage(SettingsPage.Library);
        IsVisible = true;
    }

    public void PreloadGenreVocabulary() => EnsureGenreVocabularyLoaded();

    private void EnsureGenreVocabularyLoaded()
    {
        if (_genreVocabularyLoaded)
            return;

        _isLoading = true;
        _modelGenres = MusicLibraryService.Current.GetModelGenres();
        _modelSubgenres = MusicLibraryService.Current.GetModelSubgenres();
        _distinctionsBySubgenreId = MusicLibraryService.Current.GetModelSubgenreDistinctions()
            .GroupBy(item => item.ModelSubgenreId)
            .ToDictionary(group => group.Key, group => group.ToList());

        ModelGenreBox.ItemsSource = new[] { new ModelGenreChoice(null, "All categories") }
            .Concat(_modelGenres.Select(genre => new ModelGenreChoice(genre.Id, genre.Name)))
            .ToList();
        ModelGenreBox.SelectedIndex = 0;
        SearchBox.Text = string.Empty;
        _genreVocabularyLoaded = true;
        _isLoading = false;
        BuildGenreVocabularyRowCache();
    }

    private void RebuildGenreVocabularyRows()
    {
        if (_isLoading || !_genreVocabularyLoaded) return;

        var search = SearchBox.Text?.Trim() ?? string.Empty;
        var selectedModelGenreId = (ModelGenreBox.SelectedItem as ModelGenreChoice)?.Id;
        var visibleCount = 0;
        foreach (var row in _genreVocabularyRowsById.Values)
        {
            var isVisible = (selectedModelGenreId is null || row.Subgenre.ModelGenreId == selectedModelGenreId)
                            && MatchesSearch(row.Subgenre, row.ModelGenreName, search);
            row.Control.IsVisible = isVisible;
            if (isVisible) visibleCount++;
        }

        if (_emptyGenreVocabularyText is not null)
            _emptyGenreVocabularyText.IsVisible = visibleCount == 0;
    }

    private void BuildGenreVocabularyRowCache()
    {
        GenreVocabularyRows.Children.Clear();
        _genreVocabularyRowsById.Clear();
        var modelGenreNames = _modelGenres.ToDictionary(genre => genre.Id, genre => genre.Name);
        foreach (var subgenre in _modelSubgenres
                     .OrderBy(subgenre => modelGenreNames.GetValueOrDefault(subgenre.ModelGenreId, ""), StringComparer.OrdinalIgnoreCase)
                     .ThenBy(subgenre => subgenre.Name, StringComparer.OrdinalIgnoreCase))
        {
            var row = CreateGenreVocabularyRow(subgenre, modelGenreNames[subgenre.ModelGenreId]);
            _genreVocabularyRowsById[subgenre.Id] = row;
            GenreVocabularyRows.Children.Add(row.Control);
        }

        _emptyGenreVocabularyText = new TextBlock
        {
            Text = "No genres match the current filter.",
            Opacity = 0.52,
            Margin = new Avalonia.Thickness(0, 18, 0, 0),
            IsVisible = false
        };
        GenreVocabularyRows.Children.Add(_emptyGenreVocabularyText);
        RebuildGenreVocabularyRows();
    }

    private bool MatchesSearch(ModelSubgenre subgenre, string modelGenreName, string search)
    {
        if (search.Length == 0) return true;
        return modelGenreName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || subgenre.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || (subgenre.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
               || (subgenre.ClassificationHint?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private GenreVocabularyRowState CreateGenreVocabularyRow(ModelSubgenre subgenre, string modelGenreName)
    {
        var row = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#111419")),
            BorderBrush = new SolidColorBrush(Color.Parse("#26313A")),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(7),
            Padding = new Avalonia.Thickness(12, 10)
        };
        var panel = new StackPanel { Spacing = 10 };
        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        var titleText = new TextBlock
        {
            Text = $"{modelGenreName}  ·  {subgenre.Name}",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#E8F0F6")),
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        header.Children.Add(titleText);
        var bpm = new TextBlock
        {
            Text = BpmText(subgenre),
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse("#9FCBE4")),
            Opacity = 0.86,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(bpm, 1);
        header.Children.Add(bpm);
        panel.Children.Add(header);

        var nameBox = new TextBox { Text = subgenre.Name, Height = 32, Watermark = "Subgenre name" };
        var descriptionBox = new TextBox { Text = subgenre.Description ?? string.Empty, Height = 32, Watermark = "Short description" };
        var hintBox = new TextBox { Text = subgenre.ClassificationHint ?? string.Empty, Height = 32, Watermark = "Classification hint" };
        var bpmMinBox = new TextBox { Text = subgenre.BpmMin?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Height = 32, Watermark = "Min" };
        var bpmMaxBox = new TextBox { Text = subgenre.BpmMax?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, Height = 32, Watermark = "Max" };

        var topFields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,82,82,Auto"), ColumnSpacing = 8 };
        topFields.Children.Add(CreateLabeledField("Subgenre", nameBox));
        Grid.SetColumn(bpmMinBox, 1);
        Grid.SetColumn(bpmMaxBox, 2);
        topFields.Children.Add(CreateLabeledField("BPM min", bpmMinBox, 1));
        topFields.Children.Add(CreateLabeledField("BPM max", bpmMaxBox, 2));
        var save = new Button
        {
            Content = "Save",
            Padding = new Avalonia.Thickness(12, 5),
            FontSize = 10.5,
            Background = new SolidColorBrush(Color.Parse("#26313A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#425365")),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Grid.SetColumn(save, 3);
        topFields.Children.Add(save);
        panel.Children.Add(topFields);
        panel.Children.Add(CreateLabeledField("Description", descriptionBox));
        panel.Children.Add(CreateLabeledField("Classification hint", hintBox));

        var details = new List<string>();
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
            panel.Children.Add(detailsText);
        }

        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameBox.Text))
            {
                ToastRequested?.Invoke("Genre name is required.");
                return;
            }
            if (!TryParseNullableInt(bpmMinBox.Text, out var bpmMin) || !TryParseNullableInt(bpmMaxBox.Text, out var bpmMax))
            {
                ToastRequested?.Invoke("BPM values must be whole numbers.");
                return;
            }
            MusicLibraryService.Current.UpdateModelSubgenre(
                subgenre.Id,
                nameBox.Text,
                descriptionBox.Text,
                hintBox.Text,
                bpmMin,
                bpmMax);
            var updated = subgenre with
            {
                Name = nameBox.Text.Trim(),
                Description = descriptionBox.Text,
                ClassificationHint = hintBox.Text,
                BpmMin = bpmMin,
                BpmMax = bpmMax
            };
            var index = _modelSubgenres.FindIndex(item => item.Id == subgenre.Id);
            if (index >= 0) _modelSubgenres[index] = updated;
            titleText.Text = $"{modelGenreName}  ·  {updated.Name}";
            bpm.Text = BpmText(updated);
            if (_genreVocabularyRowsById.TryGetValue(updated.Id, out var state))
                state.Subgenre = updated;
            ToastRequested?.Invoke("Genre updated.");
            RebuildGenreVocabularyRows();
            UpdateSummary();
            LibraryMetadataChanged?.Invoke();
        };

        row.Child = panel;
        return new GenreVocabularyRowState(row, subgenre, modelGenreName);
    }

    private static StackPanel CreateLabeledField(string label, Control field, int? gridColumn = null)
    {
        var panel = new StackPanel { Spacing = 3 };
        panel.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 9.5,
            Opacity = 0.45,
            FontWeight = FontWeight.SemiBold
        });
        panel.Children.Add(field);
        if (gridColumn is int column)
            Grid.SetColumn(panel, column);
        return panel;
    }

    private static string BpmText(ModelSubgenre subgenre) => subgenre.BpmMin is not null && subgenre.BpmMax is not null
        ? $"Typical BPM · {subgenre.BpmMin}–{subgenre.BpmMax}"
        : subgenre.BpmMin is not null ? $"Typical BPM · from {subgenre.BpmMin}"
        : subgenre.BpmMax is not null ? $"Typical BPM · up to {subgenre.BpmMax}"
        : string.Empty;

    private static bool TryParseNullableInt(string? text, out int? value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = null;
            return true;
        }
        if (int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            || int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
        {
            value = parsed;
            return true;
        }
        value = null;
        return false;
    }

    private void ReloadGenreVocabulary()
    {
        var selectedCategoryId = (ModelGenreBox.SelectedItem as ModelGenreChoice)?.Id;
        _genreVocabularyLoaded = true;
        _isLoading = true;
        _modelGenres = MusicLibraryService.Current.GetModelGenres();
        _modelSubgenres = MusicLibraryService.Current.GetModelSubgenres();
        _distinctionsBySubgenreId = MusicLibraryService.Current.GetModelSubgenreDistinctions()
            .GroupBy(item => item.ModelSubgenreId)
            .ToDictionary(group => group.Key, group => group.ToList());

        ModelGenreBox.ItemsSource = new[] { new ModelGenreChoice(null, "All categories") }
            .Concat(_modelGenres.Select(genre => new ModelGenreChoice(genre.Id, genre.Name)))
            .ToList();
        ModelGenreBox.SelectedItem = ((IEnumerable<ModelGenreChoice>)ModelGenreBox.ItemsSource!)
            .FirstOrDefault(choice => choice.Id == selectedCategoryId)
            ?? ((IEnumerable<ModelGenreChoice>)ModelGenreBox.ItemsSource!).First();
        _isLoading = false;
        BuildGenreVocabularyRowCache();
        UpdateSummary();
        LibraryMetadataChanged?.Invoke();
    }

    private void OnAddSubgenreClicked(object? sender, RoutedEventArgs e)
    {
        if ((ModelGenreBox.SelectedItem as ModelGenreChoice)?.Id is not int categoryId)
        {
            ToastRequested?.Invoke("Select a category before adding a subgenre.");
            return;
        }
        var name = NewSubgenreBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ToastRequested?.Invoke("Subgenre name is required.");
            return;
        }

        try
        {
            MusicLibraryService.Current.AddModelSubgenre(categoryId, name);
            NewSubgenreBox.Text = string.Empty;
            ToastRequested?.Invoke($"Added {name}.");
            ReloadGenreVocabulary();
        }
        catch (Exception exception)
        {
            ToastRequested?.Invoke($"Could not add genre: {exception.Message}");
        }
    }

    private void OnSettingsNavigationClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton { Tag: string value }) return;
        SelectPage(value switch
        {
            "library" => SettingsPage.Library,
            "export" => SettingsPage.Export,
            "calibration" => SettingsPage.AnalysisCalibration,
            "tags" => SettingsPage.Tags,
            "tag_rules" => SettingsPage.TagRules,
            _ => SettingsPage.GenreVocabulary
        });
    }

    private void SelectPage(SettingsPage page)
    {
        if (page == SettingsPage.GenreVocabulary)
            EnsureGenreVocabularyLoaded();

        _selectedPage = page;
        var isGenreVocabularyPage = page == SettingsPage.GenreVocabulary;
        var isLibraryPage = page == SettingsPage.Library;
        var isExportPage = page == SettingsPage.Export;
        GenreVocabularyPage.IsVisible = isGenreVocabularyPage;
        LibraryPage.IsVisible = isLibraryPage;
        ExportPage.IsVisible = isExportPage;
        AnalysisCalibrationPage.IsVisible = page == SettingsPage.AnalysisCalibration;
        TagsPage.IsVisible = page == SettingsPage.Tags;
        TagRulesPage.IsVisible = page == SettingsPage.TagRules;
        GenreVocabularyNavButton.IsChecked = isGenreVocabularyPage;
        LibraryNavButton.IsChecked = isLibraryPage;
        ExportNavButton.IsChecked = isExportPage;
        AnalysisCalibrationNavButton.IsChecked = page == SettingsPage.AnalysisCalibration;
        TagsNavButton.IsChecked = page == SettingsPage.Tags;
        TagRulesNavButton.IsChecked = page == SettingsPage.TagRules;

        PageTitleText.Text = page switch
        {
            SettingsPage.Library => "Library",
            SettingsPage.Export => "Export",
            SettingsPage.AnalysisCalibration => "Analysis calibration",
            SettingsPage.Tags => "Tags",
            SettingsPage.TagRules => "Tag rules",
            _ => "Genres"
        };
        PageDescriptionText.Text = isGenreVocabularyPage
            ? "Review the genre categories and subgenres used directly by the library."
            : isLibraryPage
                ? "Where this installation keeps the local music library and its database."
                : isExportPage
                    ? "Export the current library into a portable folder."
                : page == SettingsPage.Tags
                    ? "Maintain your curated labels. Tags can describe mood, themes, situations or workflow states without turning them into genres."
                    : page == SettingsPage.TagRules
                        ? "Turn model signals into reviewable tag suggestions. Rules never assign tags automatically in this first version."
                        : "Compare current system interpretations before turning them into filters.";
        SummaryText.Text = isGenreVocabularyPage ? BuildSummaryText() : "";
        if (isGenreVocabularyPage) RebuildGenreVocabularyRows();
        if (page == SettingsPage.AnalysisCalibration) RebuildCalibrationRows();
        if (page == SettingsPage.Tags) ReloadTagManagement(SelectedTagCategoryId());
        if (page == SettingsPage.TagRules) ReloadTagRules();
    }

    private void OnExportRequestedClicked(object? sender, RoutedEventArgs e) => ExportRequested?.Invoke();

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
        _tagRuleGroups = MusicLibraryService.Current.GetTagRuleGroups();

        TagRuleTagBox.ItemsSource = _tags
            .Select(tag => new TagRuleTagChoice(tag.Id, tag.CategoryName, tag.Name, tag.CategoryColor))
            .ToList();
        TagRuleModelBox.ItemsSource = _tagSignalSources
            .Select(source => source.ModelName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(TagRuleModelDisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(model => new TagRuleModelChoice(model))
            .ToList();
        TagRuleMatchModeBox.ItemsSource = new[]
        {
            new TagRuleMatchModeChoice(TagRuleMatchMode.All, "ALL"),
            new TagRuleMatchModeChoice(TagRuleMatchMode.Any, "ANY")
        };

        if (TagRuleTagBox.SelectedIndex < 0 && _tags.Count > 0)
            TagRuleTagBox.SelectedIndex = 0;
        if (TagRuleModelBox.SelectedIndex < 0 && _tagSignalSources.Count > 0)
            TagRuleModelBox.SelectedIndex = 0;
        if (TagRuleMatchModeBox.SelectedIndex < 0)
            TagRuleMatchModeBox.SelectedIndex = 0;
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

        foreach (var group in _tagRuleGroups)
        {
            var accent = CategoryBrush(group.CategoryColor);
            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#111419")),
                BorderBrush = new SolidColorBrush(Color.Parse("#26313A")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(10, 8)
            };
            var panel = new StackPanel { Spacing = 7 };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("180,110,*,Auto,Auto"), ColumnSpacing = 10 };
            var tag = new TextBlock
            {
                Text = $"{group.CategoryName} · {group.TagName}",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = accent,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var matchMode = new ComboBox
            {
                ItemsSource = new[]
                {
                    new TagRuleMatchModeChoice(TagRuleMatchMode.All, "ALL"),
                    new TagRuleMatchModeChoice(TagRuleMatchMode.Any, "ANY")
                },
                SelectedItem = new TagRuleMatchModeChoice(group.MatchMode, group.MatchMode == TagRuleMatchMode.All ? "ALL" : "ANY"),
                Height = 28,
                FontSize = 10
            };
            matchMode.SelectionChanged += (_, _) =>
            {
                if (matchMode.SelectedItem is not TagRuleMatchModeChoice selected || selected.Mode == group.MatchMode)
                    return;
                MusicLibraryService.Current.SetTagRuleGroupMatchMode(group.Id, selected.Mode);
                MusicLibraryService.Current.RefreshAllTagSuggestions();
                ReloadTagRules();
            };
            var enabled = new ToggleSwitch
            {
                IsChecked = group.Enabled,
                OnContent = "On",
                OffContent = "Off",
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            enabled.IsCheckedChanged += (_, _) =>
            {
                MusicLibraryService.Current.SetTagRuleGroupEnabled(group.Id, enabled.IsChecked == true);
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
                MusicLibraryService.Current.DeleteTagRuleGroup(group.Id);
                MusicLibraryService.Current.RefreshAllTagSuggestions();
                ReloadTagRules();
                ToastRequested?.Invoke("Tag rule group deleted.");
            };
            Grid.SetColumn(matchMode, 1);
            Grid.SetColumn(enabled, 3);
            Grid.SetColumn(remove, 4);
            grid.Children.Add(tag);
            grid.Children.Add(matchMode);
            grid.Children.Add(enabled);
            grid.Children.Add(remove);
            panel.Children.Add(grid);

            foreach (var condition in group.Conditions)
                panel.Children.Add(CreateTagRuleConditionRow(group, condition, accent));

            panel.Children.Add(CreateTagRuleConditionEditor(group, accent));
            card.Child = panel;
            TagRuleRows.Children.Add(card);
        }

        if (_tagRuleGroups.Count == 0)
            TagRuleRows.Children.Add(new TextBlock { Text = "No rule groups yet. Select a tag, choose ALL or ANY, then add the first condition.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
    }

    private void OnAddTagRuleClicked(object? sender, RoutedEventArgs e)
    {
        if (TagRuleTagBox.SelectedItem is not TagRuleTagChoice tag
            || TagRuleSignalBox.SelectedItem is not TagRuleSignalChoice source
            || TagRuleMatchModeBox.SelectedItem is not TagRuleMatchModeChoice matchMode)
            return;

        if (!TryParseTagRuleThreshold(TagRuleThresholdBox.Text, out var threshold))
        {
            ToastRequested?.Invoke("Threshold must be a valid number.");
            return;
        }

        try
        {
            MusicLibraryService.Current.CreateTagRuleGroup(tag.Id, matchMode.Mode, source.ModelName, source.SignalKey, threshold);
            MusicLibraryService.Current.RefreshAllTagSuggestions();
            ReloadTagRules();
            ToastRequested?.Invoke($"Rule group created for {tag.Name}.");
        }
        catch (Exception exception) { ToastRequested?.Invoke($"Could not add tag rule: {exception.Message}"); }
    }

    private Control CreateTagRuleConditionRow(TagRuleGroup group, TagRuleCondition condition, IBrush accent)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,76,Auto"), ColumnSpacing = 10, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        var source = new TextBlock
        {
            Text = $"{TagRuleModelDisplayName(condition.SourceType)} · {condition.SourceKey}",
            FontSize = 10.5,
            Opacity = .72,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var threshold = new TextBlock
        {
            Text = $"≥ {condition.Threshold:0.###}",
            FontSize = 10.5,
            Foreground = accent,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var remove = new Button
        {
            Content = "×",
            FontSize = 12,
            Padding = new Avalonia.Thickness(7, 1),
            Background = new SolidColorBrush(Colors.Transparent),
            Opacity = .6
        };
        ToolTip.SetTip(remove, "Remove condition");
        remove.Click += (_, _) =>
        {
            MusicLibraryService.Current.DeleteTagRuleCondition(condition.Id);
            MusicLibraryService.Current.RefreshAllTagSuggestions();
            ReloadTagRules();
        };
        Grid.SetColumn(threshold, 1);
        Grid.SetColumn(remove, 2);
        row.Children.Add(source);
        row.Children.Add(threshold);
        row.Children.Add(remove);
        return row;
    }

    private Control CreateTagRuleConditionEditor(TagRuleGroup group, IBrush accent)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("165,*,78,Auto"),
            ColumnSpacing = 8,
            Margin = new Avalonia.Thickness(8, 1, 0, 0)
        };
        var modelBox = new ComboBox
        {
            ItemsSource = _tagSignalSources
                .Select(source => source.ModelName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(TagRuleModelDisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(model => new TagRuleModelChoice(model))
                .ToList(),
            Height = 29,
            FontSize = 10
        };
        var signalBox = new ComboBox { Height = 29, FontSize = 10 };
        var thresholdBox = new TextBox { Text = "0.30", Height = 29, FontSize = 10, VerticalContentAlignment = VerticalAlignment.Center };
        var add = new Button
        {
            Content = "+ condition",
            FontSize = 10,
            Padding = new Avalonia.Thickness(9, 3),
            BorderBrush = accent
        };

        void RebuildSignals()
        {
            if (modelBox.SelectedItem is not TagRuleModelChoice model)
            {
                signalBox.ItemsSource = Array.Empty<TagRuleSignalChoice>();
                return;
            }

            signalBox.ItemsSource = _tagSignalSources
                .Where(source => string.Equals(source.ModelName, model.ModelName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(source => source.SignalKey, StringComparer.OrdinalIgnoreCase)
                .Select(source => new TagRuleSignalChoice(source.ModelName, source.SignalKey, source.Description))
                .ToList();
            signalBox.SelectedIndex = 0;
        }

        modelBox.SelectionChanged += (_, _) => RebuildSignals();
        add.Click += (_, _) =>
        {
            if (signalBox.SelectedItem is not TagRuleSignalChoice signal
                || !TryParseTagRuleThreshold(thresholdBox.Text, out var threshold))
            {
                ToastRequested?.Invoke("Threshold must be a valid number.");
                return;
            }

            MusicLibraryService.Current.AddTagRuleCondition(group.Id, signal.ModelName, signal.SignalKey, threshold);
            MusicLibraryService.Current.RefreshAllTagSuggestions();
            ReloadTagRules();
        };
        modelBox.SelectedIndex = 0;
        RebuildSignals();

        Grid.SetColumn(signalBox, 1);
        Grid.SetColumn(thresholdBox, 2);
        Grid.SetColumn(add, 3);
        row.Children.Add(modelBox);
        row.Children.Add(signalBox);
        row.Children.Add(thresholdBox);
        row.Children.Add(add);
        return row;
    }

    private static bool TryParseTagRuleThreshold(string? value, out double threshold)
    {
        var parsed = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)
            || double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out threshold);
        return parsed && double.IsFinite(threshold) && threshold >= 0;
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

    private void UpdateSummary()
    {
        if (_selectedPage != SettingsPage.GenreVocabulary) return;
        SummaryText.Text = BuildSummaryText();
    }

    private string BuildSummaryText()
    {
        var selected = ModelGenreBox.SelectedItem as ModelGenreChoice;
        var relevantSubgenres = selected?.Id is int modelGenreId
            ? _modelSubgenres.Where(subgenre => subgenre.ModelGenreId == modelGenreId).ToList()
            : _modelSubgenres;
        var scope = selected?.Id is null ? "All categories" : selected!.Name;
        return $"{scope}: {relevantSubgenres.Count} genres.";
    }

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
        "moods mirex" => "MIREX mood clusters",
        "genre electronic" => "Electronic character",
        "danceability classifier" => "Danceability",
        "voice/instrumental classifiers" => "Voice / instrumental",
        _ => modelName
    };

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => IsVisible = false;

    private sealed record ModelGenreChoice(int? Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class GenreVocabularyRowState
    {
        public GenreVocabularyRowState(Control control, ModelSubgenre subgenre, string modelGenreName)
        {
            Control = control;
            Subgenre = subgenre;
            ModelGenreName = modelGenreName;
        }

        public Control Control { get; }
        public ModelSubgenre Subgenre { get; set; }
        public string ModelGenreName { get; }
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

    private sealed record TagRuleMatchModeChoice(TagRuleMatchMode Mode, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record CalibrationRow(MusicTrack Track, List<DerivedTrackAttribute> Attributes, IReadOnlyList<ExperimentalAnalysisModel> Signals)
    {
        public string Value(string key) => Attributes.FirstOrDefault(attribute => attribute.Key == key)?.EffectiveValue ?? "—";
        public string Evidence => string.Join(" · ", Signals.SelectMany(model => model.Values.Select(value => (model.Model, value)))
            .OrderByDescending(item => item.value.Score).Take(3).Select(item => $"{item.Model}: {item.value.Label} {item.value.Score:0.##}"));
    }

    private enum SettingsPage { GenreVocabulary, Library, Export, AnalysisCalibration, Tags, TagRules }
}
