using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Svg.Skia;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class SettingsOverlay : UserControl
{
    private List<ModelGenre> _modelGenres = [];
    private List<ModelSubgenre> _modelSubgenres = [];
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private List<Tag> _tags = [];
    private List<TagSignalSource> _tagSignalSources = [];
    private List<TagRuleGroup> _tagRuleGroups = [];
    private bool _isLoading;
    private bool _genreVocabularyLoaded;
    private readonly Dictionary<int, GenreVocabularyRowState> _genreVocabularyRowsById = [];
    private TextBlock? _emptyGenreVocabularyText;
    private SettingsPage _selectedPage;
    private readonly DispatcherTimer _appearanceSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly List<Action<AppearanceSettings>> _appearanceControlRefreshers = [];
    private AppearanceSettings _appearanceSettings = AppearanceSettings.Balanced();
    private bool _synchronizingAppearanceControls;
    private bool _appearanceSavePending;
    private Color _previewPrimary = Color.Parse("#5865B8");
    private Color _previewSecondary = Color.Parse("#8051AE");
    private double _previewEnergy;
    private double _previewBass;
    private double _previewTreble;

    public event Action<string>? ToastRequested;
    public event Action<MusicTrack>? TrackCalibrationRequested;
    public event Action? LibraryMetadataChanged;
    public event Action? ExportRequested;
    public event Action<AppearanceSettings>? AppearanceChanged;

    public SettingsOverlay()
    {
        InitializeComponent();
        BuildAppearanceControls();
        _appearanceSaveTimer.Tick += (_, _) => PersistPendingAppearance();
        SearchBox.TextChanged += (_, _) => RebuildGenreVocabularyRows();
        ModelGenreBox.SelectionChanged += (_, _) =>
        {
            RebuildGenreVocabularyRows();
            UpdateSummary();
        };
        CalibrationSortBox.ItemsSource = new[] { "Recently added", "Tone", "Energy", "Intensity" };
        CalibrationSortBox.SelectedIndex = 0;
        CalibrationSortBox.SelectionChanged += (_, _) => RebuildCalibrationRows();
        AppUpdateService.Current.StateChanged += OnAppUpdateStateChanged;
        RefreshUpdatePage(AppUpdateService.Current.State);
    }

    public void Open()
    {
        var appSettings = AppSettingsStore.Load();
        var locations = Values.GetConfiguredLibraryLocations();
        DatabasePathBox.Text = locations.DatabasePath;
        TracksPathBox.Text = locations.TracksDirectory;
        LibraryLocationStatusText.IsVisible = false;
        MusicAnalysisServerUrlBox.Text = appSettings.MusicAnalysisServerUrl;
        _appearanceSettings = appSettings.Appearance.Clone().Clamp();
        RefreshAppearanceControls();
        RefreshAppearancePreview();
        AnalysisServerStatusText.IsVisible = false;
        FirefoxCookiesToggle.IsChecked = Values.UseFirefoxCookiesForYtDlp;
        RefreshLinuxDependencies();
        RebuildBackupDirectoryRows();
        SelectPage(ParseSettingsPage(appSettings.LastSettingsPage));
        IsVisible = true;
    }

    private void OnRefreshLinuxDependenciesClicked(object? sender, RoutedEventArgs e) =>
        RefreshLinuxDependencies();

    private void RefreshLinuxDependencies()
    {
        LinuxDependenciesPanel.IsVisible = OperatingSystem.IsLinux();
        if (!OperatingSystem.IsLinux())
            return;

        LinuxDependenciesText.Text = string.Join(
            Environment.NewLine,
            LinuxRuntimeDependencies.Inspect().Select(dependency =>
                $"{(dependency.IsAvailable ? "OK" : "MISSING"),-7} {dependency.Name,-9} {dependency.Detail}"));
    }

    private async void OnChooseDatabaseDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose database folder",
            AllowMultiple = false
        });
        if (folders.Count == 0)
            return;

        var currentFileName = Path.GetFileName(DatabasePathBox.Text?.Trim());
        DatabasePathBox.Text = Path.Combine(
            folders[0].Path.LocalPath,
            string.IsNullOrWhiteSpace(currentFileName) ? "music.db" : currentFileName);
    }

    private async void OnChooseTracksDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose tracks folder",
            AllowMultiple = false
        });
        if (folders.Count > 0)
            TracksPathBox.Text = folders[0].Path.LocalPath;
    }

    private void OnSaveLibraryLocationsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            Values.SaveLibraryLocations(
                TracksPathBox.Text ?? string.Empty,
                DatabasePathBox.Text ?? string.Empty);

            LibraryLocationStatusText.Text = "Locations saved. Restart Music to use them.";
            LibraryLocationStatusText.IsVisible = true;
            ToastRequested?.Invoke("Library locations saved · restart Music to apply");
        }
        catch (Exception exception)
        {
            LibraryLocationStatusText.Text = $"Could not save locations: {exception.Message}";
            LibraryLocationStatusText.IsVisible = true;
        }
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
        var selectedModelGenreName = (ModelGenreBox.SelectedItem as ModelGenreChoice)?.Name;
        AddSubgenrePanel.IsVisible = selectedModelGenreId is not null;
        AddSubgenreTitleText.Text = selectedModelGenreId is null
            ? "Add subgenre"
            : $"Add subgenre to {selectedModelGenreName}";
        GenreVocabularyHintText.Text = selectedModelGenreId is null
            ? "Choose a category to add new subgenres."
            : "Cards are compact; edit only when needed.";
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
               || subgenre.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private GenreVocabularyRowState CreateGenreVocabularyRow(ModelSubgenre subgenre, string modelGenreName)
    {
        var row = new Border
        {
            Background = ThemeResources.Brush("Theme.Brush.SurfaceTranslucent"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(7),
            Padding = new Avalonia.Thickness(12, 9)
        };
        var panel = new StackPanel { Spacing = 8 };
        var current = subgenre;
        StackPanel? editorPanel = null;

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 10 };
        var titlePanel = new StackPanel { Spacing = 1 };
        var titleText = new TextBlock
        {
            Text = subgenre.Name,
            FontSize = 12.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        titlePanel.Children.Add(titleText);
        titlePanel.Children.Add(new TextBlock
        {
            Text = modelGenreName,
            FontSize = 10,
            Opacity = 0.55,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        });
        header.Children.Add(titlePanel);
        var bpm = new TextBlock
        {
            Text = BpmText(subgenre),
            FontSize = 10.5,
            Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
            Opacity = 0.86,
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(bpm, 1);
        header.Children.Add(bpm);
        var edit = new Button
        {
            Content = CreateSvgIcon("/Assets/pencil-simple.svg", 14),
            Classes = { "settings-ghost" },
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(edit, "Edit genre");
        Grid.SetColumn(edit, 2);
        header.Children.Add(edit);
        panel.Children.Add(header);

        var descriptionText = CreateGenreBodyText(subgenre.Description, "No description yet.");
        panel.Children.Add(descriptionText);
        var hintText = CreateGenreBodyText(subgenre.ClassificationHint, "No classification hint yet.");
        panel.Children.Add(hintText);

        if (_distinctionsBySubgenreId.TryGetValue(subgenre.Id, out var distinctions) && distinctions.Count > 0)
        {
            var distinctionPanel = new StackPanel
            {
                Spacing = 3,
                Margin = new Avalonia.Thickness(0, 2, 0, 0)
            };
            distinctionPanel.Children.Add(new TextBlock
            {
                Text = "Distinguish from",
                FontSize = 9.7,
                FontWeight = FontWeight.SemiBold,
                Foreground = ThemeResources.Brush("Theme.Brush.Accent"),
                Opacity = 0.72
            });
            foreach (var item in distinctions)
            {
                distinctionPanel.Children.Add(new TextBlock
                {
                    Text = $"{item.ModelSubgenreName} ({item.ModelGenreName}) — {item.Difference}",
                    FontSize = 10,
                    Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
                    Opacity = 0.78,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            panel.Children.Add(distinctionPanel);
        }

        edit.Click += (_, _) =>
        {
            if (editorPanel is null)
            {
                editorPanel = CreateGenreVocabularyEditor(
                    current,
                    updated =>
                    {
                        current = updated;
                        titleText.Text = updated.Name;
                        descriptionText.Text = BodyText(updated.Description, "No description yet.");
                        descriptionText.Opacity = string.IsNullOrWhiteSpace(updated.Description) ? 0.45 : 0.76;
                        hintText.Text = BodyText(updated.ClassificationHint, "No classification hint yet.");
                        hintText.Opacity = string.IsNullOrWhiteSpace(updated.ClassificationHint) ? 0.45 : 0.78;
                        bpm.Text = BpmText(updated);
                        if (_genreVocabularyRowsById.TryGetValue(updated.Id, out var state))
                            state.Subgenre = updated;
                    });
                panel.Children.Add(editorPanel);
            }

            editorPanel.IsVisible = !editorPanel.IsVisible;
            edit.Opacity = editorPanel.IsVisible ? 1 : 0.82;
            ToolTip.SetTip(edit, editorPanel.IsVisible ? "Close editor" : "Edit genre");
        };

        row.Child = panel;
        return new GenreVocabularyRowState(row, subgenre, modelGenreName);
    }

    private StackPanel CreateGenreVocabularyEditor(ModelSubgenre subgenre, Action<ModelSubgenre> onSaved)
    {
        var nameBox = CreateSettingsTextBox(subgenre.Name, "Subgenre name");
        var descriptionBox = CreateSettingsTextBox(subgenre.Description ?? string.Empty, "Short description");
        var hintBox = CreateSettingsTextBox(subgenre.ClassificationHint ?? string.Empty, "Classification hint");
        var bpmMinBox = CreateSettingsTextBox(subgenre.BpmMin?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, "Min");
        var bpmMaxBox = CreateSettingsTextBox(subgenre.BpmMax?.ToString(CultureInfo.InvariantCulture) ?? string.Empty, "Max");

        var editor = new StackPanel
        {
            Spacing = 8,
            IsVisible = false,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        var topFields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,82,82,Auto"), ColumnSpacing = 8 };
        topFields.Children.Add(CreateLabeledField("Subgenre", nameBox));
        topFields.Children.Add(CreateLabeledField("BPM min", bpmMinBox, 1));
        topFields.Children.Add(CreateLabeledField("BPM max", bpmMaxBox, 2));
        var save = new Button
        {
            Content = "Save",
            Classes = { "settings-action" },
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Grid.SetColumn(save, 3);
        topFields.Children.Add(save);
        editor.Children.Add(topFields);
        editor.Children.Add(CreateLabeledField("Description", descriptionBox));
        editor.Children.Add(CreateLabeledField("Classification hint", hintBox));

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

            MusicLibraryService.Current.UpdateModelSubgenre(subgenre.Id, nameBox.Text, descriptionBox.Text, hintBox.Text, bpmMin, bpmMax);
            var updated = subgenre with { Name = nameBox.Text.Trim(), Description = descriptionBox.Text, ClassificationHint = hintBox.Text, BpmMin = bpmMin, BpmMax = bpmMax };
            var index = _modelSubgenres.FindIndex(item => item.Id == subgenre.Id);
            if (index >= 0) _modelSubgenres[index] = updated;
            onSaved(updated);
            ToastRequested?.Invoke("Genre updated.");
            RebuildGenreVocabularyRows();
            UpdateSummary();
            LibraryMetadataChanged?.Invoke();
        };

        return editor;
    }

    private static TextBox CreateSettingsTextBox(string text, string watermark)
    {
        var box = new TextBox
        {
            Text = text,
            Watermark = watermark,
            Height = 32,
            FontSize = 11.5,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        box.Classes.Add("settings-input");
        return box;
    }

    private static Avalonia.Svg.Skia.Svg CreateSvgIcon(string path, double size) => new(new Uri("avares://Music/"))
    {
        Path = path,
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        Opacity = 0.82,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

    private static TextBlock CreateGenreBodyText(string? value, string fallback)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);
        return new TextBlock
        {
            Text = BodyText(value, fallback),
            FontSize = 10.7,
            Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary"),
            Opacity = hasValue ? 0.76 : 0.45,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private static string BodyText(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

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
        var page = value switch
        {
            "library" => SettingsPage.Library,
            "analysis_server" => SettingsPage.AnalysisServer,
            "appearance" => SettingsPage.Appearance,
            "backup" => SettingsPage.Backup,
            "export" => SettingsPage.Export,
            "runtime" => SettingsPage.Runtime,
            "updates" => SettingsPage.Updates,
            "calibration" => SettingsPage.AnalysisCalibration,
            "tags" => SettingsPage.Tags,
            "tag_rules" => SettingsPage.TagRules,
            _ => SettingsPage.GenreVocabulary
        };
        SelectPage(page);
        AppSettingsStore.SaveLastSettingsPage(SettingsPageKey(page));
    }

    private void SelectPage(SettingsPage page)
    {
        if (page == SettingsPage.GenreVocabulary)
            EnsureGenreVocabularyLoaded();

        _selectedPage = page;
        var isGenreVocabularyPage = page == SettingsPage.GenreVocabulary;
        var isLibraryPage = page == SettingsPage.Library;
        var isAnalysisServerPage = page == SettingsPage.AnalysisServer;
        var isBackupPage = page == SettingsPage.Backup;
        var isExportPage = page == SettingsPage.Export;
        var isRuntimePage = page == SettingsPage.Runtime;
        var isUpdatesPage = page == SettingsPage.Updates;
        var isAppearancePage = page == SettingsPage.Appearance;
        GenreVocabularyPage.IsVisible = isGenreVocabularyPage;
        LibraryPage.IsVisible = isLibraryPage;
        AnalysisServerPage.IsVisible = isAnalysisServerPage;
        BackupPage.IsVisible = isBackupPage;
        ExportPage.IsVisible = isExportPage;
        RuntimePage.IsVisible = isRuntimePage;
        UpdatesPage.IsVisible = isUpdatesPage;
        AppearancePage.IsVisible = isAppearancePage;
        AnalysisCalibrationPage.IsVisible = page == SettingsPage.AnalysisCalibration;
        TagsPage.IsVisible = page == SettingsPage.Tags;
        TagRulesPage.IsVisible = page == SettingsPage.TagRules;
        GenreVocabularyNavButton.IsChecked = isGenreVocabularyPage;
        LibraryNavButton.IsChecked = isLibraryPage;
        AnalysisServerNavButton.IsChecked = isAnalysisServerPage;
        BackupNavButton.IsChecked = isBackupPage;
        ExportNavButton.IsChecked = isExportPage;
        RuntimeNavButton.IsChecked = isRuntimePage;
        UpdatesNavButton.IsChecked = isUpdatesPage;
        AppearanceNavButton.IsChecked = isAppearancePage;
        AnalysisCalibrationNavButton.IsChecked = page == SettingsPage.AnalysisCalibration;
        TagsNavButton.IsChecked = page == SettingsPage.Tags;
        TagRulesNavButton.IsChecked = page == SettingsPage.TagRules;

        PageTitleText.Text = page switch
        {
            SettingsPage.Library => "Library",
            SettingsPage.AnalysisServer => "Analysis server",
            SettingsPage.Backup => "Backup",
            SettingsPage.Export => "Export",
            SettingsPage.Runtime => "Runtime",
            SettingsPage.Updates => "Updates",
            SettingsPage.Appearance => "Appearance",
            SettingsPage.AnalysisCalibration => "Analysis calibration",
            SettingsPage.Tags => "Tags",
            SettingsPage.TagRules => "Tag rules",
            _ => "Genres"
        };
        PageDescriptionText.Text = isGenreVocabularyPage
            ? "Review the genre categories and subgenres used directly by the library."
            : isLibraryPage
                ? "Where this installation keeps the local music library and its database."
                : isAnalysisServerPage
                    ? "Configure and verify the service used for track analysis."
                : isBackupPage
                    ? "Keep daily database snapshots in your backup locations."
                    : isExportPage
                        ? "Export the current library into a portable folder."
                        : isRuntimePage
                            ? "Temporary switches for this app run."
                            : isAppearancePage
                                ? "Tune artwork, blur, color and audio-reactive visuals. Changes are applied live."
                            : isUpdatesPage
                                ? "Check, download and install application releases from GitHub."
                            : page == SettingsPage.Tags
                                ? "Maintain your curated labels. Tags can describe mood, themes, situations or workflow states without turning them into genres."
                                : page == SettingsPage.TagRules
                                    ? "Turn model signals into reviewable tag suggestions. Rules never assign tags automatically in this first version."
                                    : "Compare current system interpretations before turning them into filters.";
        SummaryText.Text = isGenreVocabularyPage ? BuildSummaryText() : "";
        if (isGenreVocabularyPage) RebuildGenreVocabularyRows();
        if (isBackupPage) RebuildBackupDirectoryRows();
        if (page == SettingsPage.AnalysisCalibration) RebuildCalibrationRows();
        if (page == SettingsPage.Tags) ReloadTagManagement();
        if (page == SettingsPage.TagRules) ReloadTagRules();
        if (isUpdatesPage) RefreshUpdatePage(AppUpdateService.Current.State);
    }

    private async void OnCheckForUpdatesClicked(object? sender, RoutedEventArgs e)
        => await AppUpdateService.Current.CheckForUpdatesAsync(force: true);

    private async void OnDownloadUpdateClicked(object? sender, RoutedEventArgs e)
        => await AppUpdateService.Current.DownloadUpdateAsync();

    private void OnInstallUpdateClicked(object? sender, RoutedEventArgs e)
        => AppUpdateService.Current.ApplyUpdateAndRestart();

    private void OnAppUpdateStateChanged(AppUpdateState state)
        => Dispatcher.UIThread.Post(() => RefreshUpdatePage(state));

    private void RefreshUpdatePage(AppUpdateState state)
    {
        InstalledVersionText.Text = state.CurrentVersion;
        AvailableVersionText.Text = state.AvailableVersion is null
            ? string.Empty
            : $"Version {state.AvailableVersion} available";
        UpdateStatusText.Text = state.Message;
        UpdateProgressBar.Value = state.ProgressPercent;
        UpdateProgressBar.IsVisible = state.Phase == AppUpdatePhase.Downloading;

        var operationInProgress = state.Phase is AppUpdatePhase.Checking or AppUpdatePhase.Downloading;
        CheckForUpdatesButton.IsEnabled = !operationInProgress && state.Phase != AppUpdatePhase.NotInstalled;
        DownloadUpdateButton.IsVisible = state.Phase == AppUpdatePhase.UpdateAvailable;
        InstallUpdateButton.IsVisible = state.Phase == AppUpdatePhase.ReadyToInstall;
    }

    private void OnExportRequestedClicked(object? sender, RoutedEventArgs e) => ExportRequested?.Invoke();

    private void OnSaveAnalysisServerClicked(object? sender, RoutedEventArgs e)
    {
        if (!TrackAnalysisService.TryNormalizeServerUrl(MusicAnalysisServerUrlBox.Text, out var serverUrl))
        {
            ShowAnalysisServerStatus("Invalid address", isSuccess: false);
            return;
        }

        AppSettingsStore.SaveMusicAnalysisServerUrl(serverUrl);
        BackgroundAnalysisService.Current.NotifyServerConfigurationChanged();
        MusicAnalysisServerUrlBox.Text = serverUrl;
        ShowAnalysisServerStatus("Address saved", isSuccess: true);
        ToastRequested?.Invoke("Analysis server address saved.");
    }

    private async void OnTestAnalysisServerClicked(object? sender, RoutedEventArgs e)
    {
        if (!TrackAnalysisService.TryNormalizeServerUrl(MusicAnalysisServerUrlBox.Text, out var serverUrl))
        {
            ShowAnalysisServerStatus("Invalid address", isSuccess: false);
            return;
        }

        TestAnalysisServerButton.IsEnabled = false;
        ShowAnalysisServerStatus("Testing connection…", isSuccess: null);
        try
        {
            using var service = new TrackAnalysisService(serverUrlProvider: () => serverUrl);
            var isHealthy = await service.CheckHealthAsync();
            if (isHealthy)
            {
                AppSettingsStore.SaveMusicAnalysisServerUrl(serverUrl);
                MusicAnalysisServerUrlBox.Text = serverUrl;
                BackgroundAnalysisService.Current.NotifyServerConfigurationChanged();
            }
            ShowAnalysisServerStatus(
                isHealthy ? "Connection successful; address saved" : "Server returned an unhealthy status",
                isHealthy);
        }
        catch (MusicAnalysisException exception)
        {
            var message = exception.Kind switch
            {
                MusicAnalysisErrorKind.Timeout => "Request timed out",
                MusicAnalysisErrorKind.ConnectionError => "Server not reachable",
                MusicAnalysisErrorKind.ServerError => exception.Message,
                MusicAnalysisErrorKind.InvalidResponse => "Server returned an invalid response",
                _ => exception.Message
            };
            ShowAnalysisServerStatus(message, isSuccess: false);
        }
        finally
        {
            TestAnalysisServerButton.IsEnabled = true;
        }
    }

    private void ShowAnalysisServerStatus(string message, bool? isSuccess)
    {
        AnalysisServerStatusText.Text = message;
        AnalysisServerStatusText.Foreground = new SolidColorBrush(Color.Parse(isSuccess switch
        {
            true => "#73D59B",
            false => "#E87878",
            _ => "#C7D2AD"
        }));
        AnalysisServerStatusText.IsVisible = true;
    }

    private void OnFirefoxCookiesToggleChanged(object? sender, RoutedEventArgs e)
    {
        Values.UseFirefoxCookiesForYtDlp = FirefoxCookiesToggle.IsChecked == true;
        ToastRequested?.Invoke(Values.UseFirefoxCookiesForYtDlp
            ? "Firefox session enabled for yt-dlp."
            : "Firefox session disabled for yt-dlp.");
    }

    private async void OnAddBackupDirectoryClicked(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choose database backup folder",
            AllowMultiple = false
        });
        if (folders.Count == 0) return;

        try
        {
            DatabaseBackupService.Current.AddBackupDirectory(folders[0].Path.LocalPath);
            var backupResult = DatabaseBackupService.Current.EnsureTodayBackups();
            RebuildBackupDirectoryRows();
            ToastRequested?.Invoke(backupResult.Errors.Count > 0
                ? $"Backup folder added, but backup failed: {backupResult.Errors[0]}"
                : backupResult.Created.Count > 0
                    ? "Backup folder added; today's backup was created."
                    : "Backup folder added.");
        }
        catch (Exception exception)
        {
            ToastRequested?.Invoke($"Could not add backup folder: {exception.Message}");
        }
    }

    private void RebuildBackupDirectoryRows()
    {
        if (!IsInitialized) return;

        BackupDirectoryRows.Children.Clear();
        var directories = DatabaseBackupService.Current.GetBackupDirectories();
        if (directories.Count == 0)
        {
            BackupDirectoryRows.Children.Add(new TextBlock
            {
                Text = "No backup folder selected yet.",
                FontSize = 12,
                Opacity = 0.55
            });
            return;
        }

        foreach (var directory in directories)
            BackupDirectoryRows.Children.Add(CreateBackupDirectoryRow(directory));
    }

    private Control CreateBackupDirectoryRow(string directory)
    {
        var row = new Border
        {
            Background = ThemeResources.Brush("Theme.Brush.SurfaceTranslucent"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(7),
            Padding = new Avalonia.Thickness(12, 9)
        };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        grid.Children.Add(new TextBlock
        {
            Text = directory,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        });

        var removeButton = new Button
        {
            Content = "Remove",
            Classes = { "settings-action" },
            Padding = new Avalonia.Thickness(10, 5),
            VerticalAlignment = VerticalAlignment.Center
        };
        removeButton.Click += (_, _) =>
        {
            DatabaseBackupService.Current.RemoveBackupDirectory(directory);
            RebuildBackupDirectoryRows();
            ToastRequested?.Invoke("Backup folder removed.");
        };
        Grid.SetColumn(removeButton, 1);
        grid.Children.Add(removeButton);
        row.Child = grid;
        return row;
    }

    private void ReloadTagManagement()
    {
        if (!IsInitialized) return;
        _tags = MusicLibraryService.Current.GetTags();
        AddTagPanel.IsVisible = true;
        AddTagTitleText.Text = "Add tag";
        TagVocabularyHintText.Text = "Cards are compact; edit only when needed.";
        RebuildTagRows();
    }

    private void ReloadTagRules()
    {
        if (!IsInitialized) return;
        _tags = MusicLibraryService.Current.GetTags();
        _tagSignalSources = MusicLibraryService.Current.GetTagSignalSources();
        _tagRuleGroups = MusicLibraryService.Current.GetTagRuleGroups();

        TagRuleTagBox.ItemsSource = _tags
            .Select(tag => new TagRuleTagChoice(tag.Id, tag.Name))
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
            var accent = CategoryBrush(null);
            var card = new Border
            {
                Background = ThemeResources.Brush("Theme.Brush.Surface"),
                BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(10, 8)
            };
            var panel = new StackPanel { Spacing = 7 };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("180,110,*,Auto,Auto"), ColumnSpacing = 10 };
            var tag = new TextBlock
            {
                Text = group.TagName,
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

    private void RebuildTagRows()
    {
        if (!IsInitialized) return;
        TagRows.Children.Clear();
        var tags = _tags
            .OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var tag in tags)
        {
            TagRows.Children.Add(CreateTagVocabularyRow(tag));
        }

        if (tags.Count == 0)
            TagRows.Children.Add(new TextBlock { Text = "No tags yet.", Opacity = .52, Margin = new Avalonia.Thickness(0, 18, 0, 0) });
    }

    private Control CreateTagVocabularyRow(Tag tag)
    {
        var row = new Border
        {
            Background = ThemeResources.Brush("Theme.Brush.SurfaceTranslucent"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(7),
            Padding = new Avalonia.Thickness(12, 9)
        };
        var panel = new StackPanel { Spacing = 8 };
        var current = tag;
        StackPanel? editorPanel = null;

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
        var titlePanel = new StackPanel { Spacing = 1 };
        var accent = CategoryBrush(null);
        var titleText = new TextBlock
        {
            Text = tag.Name,
            FontSize = 12.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = accent,
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
        };
        titlePanel.Children.Add(titleText);
        header.Children.Add(titlePanel);

        var edit = new Button
        {
            Content = CreateSvgIcon("/Assets/pencil-simple.svg", 14),
            Classes = { "settings-ghost" },
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(edit, "Edit tag");
        Grid.SetColumn(edit, 1);
        header.Children.Add(edit);
        panel.Children.Add(header);

        edit.Click += (_, _) =>
        {
            if (editorPanel is null)
            {
                editorPanel = CreateTagVocabularyEditor(
                    current,
                    updated =>
                    {
                        current = updated;
                        titleText.Text = updated.Name;
                    });
                panel.Children.Add(editorPanel);
            }

            editorPanel.IsVisible = !editorPanel.IsVisible;
            edit.Opacity = editorPanel.IsVisible ? 1 : 0.82;
            ToolTip.SetTip(edit, editorPanel.IsVisible ? "Close editor" : "Edit tag");
        };

        row.Child = panel;
        return row;
    }

    private StackPanel CreateTagVocabularyEditor(Tag tag, Action<Tag> onSaved)
    {
        var nameBox = CreateSettingsTextBox(tag.Name, "Tag name");
        var editor = new StackPanel
        {
            Spacing = 8,
            IsVisible = false,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };

        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
        fields.Children.Add(CreateLabeledField("Tag", nameBox));
        var save = new Button
        {
            Content = "Save",
            Classes = { "settings-action" },
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var remove = new Button
        {
            Content = "Delete",
            Classes = { "settings-ghost" },
            Width = double.NaN,
            Padding = new Avalonia.Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Bottom,
            Opacity = .7
        };
        Grid.SetColumn(save, 1);
        Grid.SetColumn(remove, 2);
        fields.Children.Add(save);
        fields.Children.Add(remove);
        editor.Children.Add(fields);

        save.Click += (_, _) =>
        {
            try
            {
                MusicLibraryService.Current.RenameTag(tag.Id, nameBox.Text ?? tag.Name);
                var updated = tag with { Name = (nameBox.Text ?? tag.Name).Trim() };
                var index = _tags.FindIndex(item => item.Id == tag.Id);
                if (index >= 0) _tags[index] = updated;
                onSaved(updated);
                ToastRequested?.Invoke("Tag updated.");
                RebuildTagRows();
                LibraryMetadataChanged?.Invoke();
            }
            catch (Exception exception) { ToastRequested?.Invoke($"Could not update tag: {exception.Message}"); }
        };
        remove.Click += (_, _) =>
        {
            var error = MusicLibraryService.Current.DeleteTagIfUnused(tag.Id);
            ToastRequested?.Invoke(error ?? "Tag deleted.");
            ReloadTagManagement();
            if (error is null) LibraryMetadataChanged?.Invoke();
        };

        return editor;
    }

    private static bool TryNormalizeHexColor(string? value, out string? color)
    {
        color = null;
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return true;

        if (!text.StartsWith('#')) text = "#" + text;
        if (text.Length != 7)
            return false;

        for (var i = 1; i < text.Length; i++)
            if (!Uri.IsHexDigit(text[i]))
                return false;

        color = text.ToUpperInvariant();
        return true;
    }

    private void OnAddTagClicked(object? sender, RoutedEventArgs e)
    {
        var name = NewTagBox.Text?.Trim() ?? string.Empty;
        if (name.Length == 0) return;
        try
        {
            MusicLibraryService.Current.AddTag(name);
            NewTagBox.Text = string.Empty;
            ReloadTagManagement();
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
        var button = new Button { Background = ThemeResources.Brush("Theme.Brush.Surface"), BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"), BorderThickness = new Avalonia.Thickness(1), Padding = new Avalonia.Thickness(11, 9), HorizontalContentAlignment = HorizontalAlignment.Stretch };
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
        if (string.IsNullOrWhiteSpace(color))
            return ThemeResources.Brush("Theme.Brush.TextSecondary");

        try { return new SolidColorBrush(Color.Parse(color)); }
        catch { return ThemeResources.Brush("Theme.Brush.TextSecondary"); }
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

    public void UpdateAppearancePreviewArtwork(Bitmap? artwork, Color primary, Color secondary, string title)
    {
        _previewPrimary = primary;
        _previewSecondary = secondary;
        PreviewTrackTitle.Text = string.IsNullOrWhiteSpace(title) ? "Preview track" : title;
        if (artwork is not null)
        {
            PreviewLibraryArtwork.Source = artwork;
            PreviewTrackArtworkBlur.Source = artwork;
            PreviewCoverHalo.Source = artwork;
            PreviewCoverArtwork.Source = artwork;
            PreviewPlayerArtwork.Source = artwork;
        }
        RefreshAppearancePreview();
    }

    public void UpdateAppearancePreviewAudio(double energy, double bass, double treble)
    {
        _previewEnergy = energy;
        _previewBass = bass;
        _previewTreble = treble;
        PreviewSpectrumVisualizer.Advance();
        if (IsVisible && AppearancePage.IsVisible)
            RefreshAppearancePreview();
    }

    public void UpdateAppearancePreviewSpectrum(IReadOnlyList<float>? spectrum) =>
        PreviewSpectrumVisualizer.SetSpectrum(spectrum);

    private void RefreshAppearancePreview()
    {
        var reaction = _appearanceSettings.PlayerAudioReaction / 100d;
        var energy = PreviewSoftLimit(_previewEnergy) * reaction;
        var bass = PreviewSoftLimit(_previewBass * _appearanceSettings.AudioBassSensitivity / 100d) * reaction;
        var treble = PreviewSoftLimit(_previewTreble * _appearanceSettings.AudioTrebleSensitivity / 100d) * reaction;
        var visibilityEnergy = Math.Sqrt(energy);
        var motion = _appearanceSettings.AudioArtworkMotion / 100d;
        var blurReaction = _appearanceSettings.AudioBlurReaction / 100d;
        var colorReaction = _appearanceSettings.AudioColorReaction / 100d;
        var atmosphere = _appearanceSettings.PlayerColorAtmosphere / 100d;

        PreviewSpectrumVisualizer.IsVisible = _appearanceSettings.SpectrumVisualizerEnabled;
        PreviewSpectrumVisualizer.Height = _appearanceSettings.SpectrumVisualizerHeight * 0.65;
        PreviewSpectrumVisualizer.Opacity = 0.40;
        PreviewSpectrumVisualizer.Sensitivity = _appearanceSettings.SpectrumVisualizerSensitivity / 100d;
        PreviewSpectrumVisualizer.Smoothing = _appearanceSettings.SpectrumVisualizerSmoothing / 100d;
        PreviewSpectrumVisualizer.SetColors(_previewPrimary, _previewSecondary);

        PreviewLibraryArtwork.Opacity = Math.Clamp(
            _appearanceSettings.LibraryBackdropStrength / 100d + visibilityEnergy * 0.21, 0, 1);
        PreviewPlayerArtwork.Opacity = Math.Clamp(
            _appearanceSettings.PlayerArtworkStrength / 100d + visibilityEnergy * 0.15, 0, 1);
        SetPreviewBlur(PreviewLibraryArtwork,
            _appearanceSettings.LibraryBackdropBlur + energy * 8 * blurReaction);
        SetPreviewBlur(PreviewPlayerArtwork,
            _appearanceSettings.PlayerArtworkBlur + (energy * 6 + treble * 4) * blurReaction);
        SetPreviewScale(PreviewLibraryArtwork, 1.08 + bass * 0.048 * motion);
        SetPreviewScale(PreviewPlayerArtwork, 1.10 + bass * 0.035 * motion);

        PreviewTrackArtworkBlur.Opacity = _appearanceSettings.TrackArtworkStrength / 100d;
        PreviewCoverHalo.Opacity = _appearanceSettings.CoverHaloStrength / 100d;
        SetPreviewBlur(PreviewTrackArtworkBlur, _appearanceSettings.TrackArtworkBlur);
        SetPreviewBlur(PreviewCoverHalo, _appearanceSettings.CoverHaloBlur);
        PreviewTrackWash.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(_appearanceSettings.TrackColorWashReach / 100d, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(PreviewWithOpacity(_previewPrimary, _appearanceSettings.TrackColorWashStrength / 100d), 0),
                new GradientStop(PreviewWithOpacity(_previewSecondary, _appearanceSettings.TrackColorWashStrength / 100d * 0.6), 0.42),
                new GradientStop(Colors.Transparent, 1)
            }
        };

        PreviewPlayerDarkening.Background = new SolidColorBrush(PreviewWithOpacity(
            Color.Parse("#2A241A"), _appearanceSettings.PlayerBackgroundDarkening / 100d));
        var liftedPrimary = PreviewMix(_previewPrimary, Colors.White,
            (energy * 0.08 + treble * 0.05) * colorReaction);
        var liftedSecondary = PreviewMix(_previewSecondary, Colors.White, energy * 0.05 * colorReaction);
        PreviewPlayerAtmosphere.Background = PreviewAtmosphereBrush(
            liftedPrimary, liftedSecondary, (0.18 + energy * 0.20 * colorReaction) * atmosphere);
        PreviewLibraryAtmosphere.Background = PreviewAtmosphereBrush(
            liftedPrimary, liftedSecondary, (0.12 + energy * 0.14 * colorReaction) * atmosphere);

        PreviewAudioBar1.Height = 6 + bass * 12;
        PreviewAudioBar2.Height = 8 + energy * 18;
        PreviewAudioBar3.Height = 6 + treble * 14;
    }

    private static LinearGradientBrush PreviewAtmosphereBrush(Color primary, Color secondary, double opacity) => new()
    {
        StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(PreviewWithOpacity(primary, opacity), 0),
            new GradientStop(PreviewWithOpacity(secondary, opacity * 0.7), 0.6),
            new GradientStop(Colors.Transparent, 1)
        }
    };

    private static void SetPreviewBlur(Image image, double radius)
    {
        if (image.Effect is BlurEffect blur)
            blur.Radius = radius;
    }

    private static void SetPreviewScale(Image image, double scale)
    {
        if (image.RenderTransform is ScaleTransform transform)
        {
            transform.ScaleX = scale;
            transform.ScaleY = scale;
        }
    }

    private static Color PreviewWithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255), color.R, color.G, color.B);

    private static Color PreviewMix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    private static double PreviewSoftLimit(double value) =>
        Math.Clamp(1 - Math.Exp(-Math.Max(0, value) * 1.45), 0, 1);

    private static SettingsPage ParseSettingsPage(string? value) => value switch
    {
        "appearance" => SettingsPage.Appearance,
        "analysis_server" => SettingsPage.AnalysisServer,
        "backup" => SettingsPage.Backup,
        "export" => SettingsPage.Export,
        "runtime" => SettingsPage.Runtime,
        "updates" => SettingsPage.Updates,
        "genres" => SettingsPage.GenreVocabulary,
        "tags" => SettingsPage.Tags,
        _ => SettingsPage.Library
    };

    private static string SettingsPageKey(SettingsPage page) => page switch
    {
        SettingsPage.Appearance => "appearance",
        SettingsPage.AnalysisServer => "analysis_server",
        SettingsPage.Backup => "backup",
        SettingsPage.Export => "export",
        SettingsPage.Runtime => "runtime",
        SettingsPage.Updates => "updates",
        SettingsPage.GenreVocabulary => "genres",
        SettingsPage.Tags => "tags",
        _ => "library"
    };

    private void BuildAppearanceControls()
    {
        AddAppearanceSlider(PlayerAppearanceRows, "Artwork strength", "Visibility of the cover behind the player.",
            0, 100, settings => settings.PlayerArtworkStrength,
            (settings, value) => settings.PlayerArtworkStrength = value, PercentValue);
        AddAppearanceSlider(PlayerAppearanceRows, "Artwork blur", "Softness of the player background image.",
            0, 50, settings => settings.PlayerArtworkBlur,
            (settings, value) => settings.PlayerArtworkBlur = value, PixelValue);
        AddAppearanceSlider(PlayerAppearanceRows, "Background darkening", "Dark overlay that keeps controls and text readable.",
            0, 80, settings => settings.PlayerBackgroundDarkening,
            (settings, value) => settings.PlayerBackgroundDarkening = value, PercentValue);
        AddAppearanceSlider(PlayerAppearanceRows, "Color atmosphere", "Strength of colors extracted from the active cover.",
            0, 100, settings => settings.PlayerColorAtmosphere,
            (settings, value) => settings.PlayerColorAtmosphere = value, PercentValue);
        AddAppearanceSlider(TrackAppearanceRows, "Color wash strength", "Intensity of each cover's color across its track row.",
            0, 30, settings => settings.TrackColorWashStrength,
            (settings, value) => settings.TrackColorWashStrength = value, PercentValue);
        AddAppearanceSlider(TrackAppearanceRows, "Color wash reach", "How far the artwork color extends across the row.",
            20, 100, settings => settings.TrackColorWashReach,
            (settings, value) => settings.TrackColorWashReach = value, PercentValue);

        AddAppearanceSlider(AudioAppearanceRows, "Overall intensity", "How strongly playback changes opacity, blur, color and movement.",
            0, 100, settings => settings.PlayerAudioReaction,
            (settings, value) => settings.PlayerAudioReaction = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Response speed", "How quickly visuals follow changes in the music.",
            0, 100, settings => settings.AudioResponseSpeed,
            (settings, value) => settings.AudioResponseSpeed = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Bass sensitivity", "How strongly low frequencies drive movement and glow.",
            0, 200, settings => settings.AudioBassSensitivity,
            (settings, value) => settings.AudioBassSensitivity = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Treble sensitivity", "How strongly high frequencies affect blur and highlights.",
            0, 200, settings => settings.AudioTrebleSensitivity,
            (settings, value) => settings.AudioTrebleSensitivity = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Artwork motion", "Amount of cover zoom driven by the bass.",
            0, 200, settings => settings.AudioArtworkMotion,
            (settings, value) => settings.AudioArtworkMotion = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Blur reaction", "How much audio energy changes the background blur.",
            0, 200, settings => settings.AudioBlurReaction,
            (settings, value) => settings.AudioBlurReaction = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Color reaction", "How much audio energy brightens colors and glow.",
            0, 200, settings => settings.AudioColorReaction,
            (settings, value) => settings.AudioColorReaction = value, PercentValue);
        AddAppearanceToggle(AudioAppearanceRows, "Frequency visualizer", "Show the live 20 Hz - 20 kHz spectrum behind the track list.",
            settings => settings.SpectrumVisualizerEnabled,
            (settings, value) => settings.SpectrumVisualizerEnabled = value);
        AddAppearanceSlider(AudioAppearanceRows, "Visualizer height", "Maximum height of the spectrum above the player.",
            40, 220, settings => settings.SpectrumVisualizerHeight,
            (settings, value) => settings.SpectrumVisualizerHeight = value, PixelValue);
        AddAppearanceSlider(AudioAppearanceRows, "Visualizer sensitivity", "Amplifies or reduces the displayed frequency levels.",
            25, 250, settings => settings.SpectrumVisualizerSensitivity,
            (settings, value) => settings.SpectrumVisualizerSensitivity = value, PercentValue);
        AddAppearanceSlider(AudioAppearanceRows, "Visualizer smoothing", "Higher values make movement calmer and more fluid.",
            0, 95, settings => settings.SpectrumVisualizerSmoothing,
            (settings, value) => settings.SpectrumVisualizerSmoothing = value, PercentValue);

        AddAppearanceSlider(LibraryBackdropAppearanceRows, "Backdrop strength", "Visibility of the active cover behind the library.",
            0, 60, settings => settings.LibraryBackdropStrength,
            (settings, value) => settings.LibraryBackdropStrength = value, PercentValue);
        AddAppearanceSlider(LibraryBackdropAppearanceRows, "Backdrop blur", "Softness of the active cover behind the library.",
            0, 50, settings => settings.LibraryBackdropBlur,
            (settings, value) => settings.LibraryBackdropBlur = value, PixelValue);

        AddAppearanceSlider(TrackArtworkAppearanceRows, "Row artwork strength", "Visibility of the blurred cover inside each row.",
            0, 50, settings => settings.TrackArtworkStrength,
            (settings, value) => settings.TrackArtworkStrength = value, PercentValue);
        AddAppearanceSlider(TrackArtworkAppearanceRows, "Row artwork blur", "Spread of the large cover image inside each row.",
            0, 30, settings => settings.TrackArtworkBlur,
            (settings, value) => settings.TrackArtworkBlur = value, PixelValue);
        AddAppearanceSlider(TrackArtworkAppearanceRows, "Cover halo strength", "Visibility of the small glow directly around each cover.",
            0, 60, settings => settings.CoverHaloStrength,
            (settings, value) => settings.CoverHaloStrength = value, PercentValue);
        AddAppearanceSlider(TrackArtworkAppearanceRows, "Cover halo blur", "Softness of the local glow around each cover.",
            0, 20, settings => settings.CoverHaloBlur,
            (settings, value) => settings.CoverHaloBlur = value, PixelValue);
    }

    private void AddAppearanceSlider(
        StackPanel panel,
        string title,
        string description,
        double minimum,
        double maximum,
        Func<AppearanceSettings, double> getter,
        Action<AppearanceSettings, double> setter,
        Func<double, string> formatter)
    {
        var valueText = new TextBlock
        {
            Width = 48,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10.5,
            Opacity = 0.72,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            TickFrequency = 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(slider, "Double-click to restore this value to the Balanced preset.");
        slider.ValueChanged += (_, _) =>
        {
            valueText.Text = formatter(slider.Value);
            if (_synchronizingAppearanceControls)
                return;

            setter(_appearanceSettings, slider.Value);
            NotifyAppearanceChanged();
        };
        slider.DoubleTapped += (_, _) => slider.Value = getter(AppearanceSettings.Balanced());

        var labels = new StackPanel { Spacing = 2 };
        labels.Children.Add(new TextBlock { Text = title, FontSize = 11.5, FontWeight = FontWeight.SemiBold });
        labels.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Opacity = 0.5,
            TextWrapping = TextWrapping.Wrap
        });

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,220,52"),
            ColumnSpacing = 12
        };
        row.Children.Add(labels);
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);
        Grid.SetColumn(valueText, 2);
        row.Children.Add(valueText);
        panel.Children.Add(row);

        _appearanceControlRefreshers.Add(settings =>
        {
            slider.Value = getter(settings);
            valueText.Text = formatter(slider.Value);
        });
    }

    private void AddAppearanceToggle(
        StackPanel panel,
        string title,
        string description,
        Func<AppearanceSettings, bool> getter,
        Action<AppearanceSettings, bool> setter)
    {
        var toggle = new ToggleSwitch
        {
            OnContent = "On",
            OffContent = "Off",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        void UpdateValue()
        {
            if (_synchronizingAppearanceControls)
                return;
            setter(_appearanceSettings, toggle.IsChecked == true);
            NotifyAppearanceChanged();
        }
        toggle.IsCheckedChanged += (_, _) => UpdateValue();

        var labels = new StackPanel { Spacing = 2 };
        labels.Children.Add(new TextBlock { Text = title, FontSize = 11.5, FontWeight = FontWeight.SemiBold });
        labels.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Opacity = 0.5,
            TextWrapping = TextWrapping.Wrap
        });

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        row.Children.Add(labels);
        Grid.SetColumn(toggle, 1);
        row.Children.Add(toggle);
        panel.Children.Add(row);

        _appearanceControlRefreshers.Add(settings => toggle.IsChecked = getter(settings));
    }

    private void RefreshAppearanceControls()
    {
        _synchronizingAppearanceControls = true;
        try
        {
            foreach (var refresh in _appearanceControlRefreshers)
                refresh(_appearanceSettings);
        }
        finally
        {
            _synchronizingAppearanceControls = false;
        }
    }

    private void NotifyAppearanceChanged()
    {
        _appearanceSettings.Clamp();
        AppearanceChanged?.Invoke(_appearanceSettings.Clone());
        RefreshAppearancePreview();
        _appearanceSavePending = true;
        _appearanceSaveTimer.Stop();
        _appearanceSaveTimer.Start();
    }

    private void PersistPendingAppearance()
    {
        _appearanceSaveTimer.Stop();
        if (!_appearanceSavePending)
            return;

        AppSettingsStore.SaveAppearance(_appearanceSettings);
        _appearanceSavePending = false;
    }

    private void OnAppearancePresetClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string preset })
            return;

        _appearanceSettings = preset switch
        {
            "subtle" => AppearanceSettings.Subtle(),
            "vibrant" => AppearanceSettings.Vibrant(),
            _ => AppearanceSettings.Balanced()
        };
        RefreshAppearanceControls();
        NotifyAppearanceChanged();
        PersistPendingAppearance();
        ToastRequested?.Invoke($"Appearance preset applied: {preset}.");
    }

    private void OnResetAppearanceClicked(object? sender, RoutedEventArgs e)
    {
        _appearanceSettings = AppearanceSettings.Balanced();
        RefreshAppearanceControls();
        NotifyAppearanceChanged();
        PersistPendingAppearance();
        ToastRequested?.Invoke("Appearance reset to defaults.");
    }

    private static string PercentValue(double value) => $"{value:0}%";
    private static string PixelValue(double value) => $"{value:0} px";

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        PersistPendingAppearance();
        IsVisible = false;
    }

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

    private sealed record TagRuleTagChoice(int Id, string Name)
    {
        public override string ToString() => Name;
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

    private enum SettingsPage { GenreVocabulary, Library, AnalysisServer, Appearance, Backup, Export, Runtime, Updates, AnalysisCalibration, Tags, TagRules }
}
