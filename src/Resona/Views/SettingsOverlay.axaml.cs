using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using Avalonia.Svg.Skia;
using Resona.Models;
using Resona.Services;

namespace Resona.Views;

public partial class SettingsOverlay : UserControl
{
    private List<ModelGenre> _modelGenres = [];
    private List<ModelSubgenre> _modelSubgenres = [];
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private List<Tag> _tags = [];
    private Dictionary<int, int> _tagUsageCounts = [];
    private List<TagSignalSource> _tagSignalSources = [];
    private List<TagRuleGroup> _tagRuleGroups = [];
    private bool _isLoading;
    private bool _genreVocabularyLoaded;
    private int? _selectedModelGenreId;
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
    private bool _loadingDiscordPresenceSettings;
    private bool _loadingProfile;
    private bool _profileSavePending;
    private readonly DispatcherTimer _profileSaveTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly DispatcherTimer _profileSaveAnimationTimer = new() { Interval = TimeSpan.FromMilliseconds(45) };
    private CloudIdentity? _cloudIdentity;
    private byte[]? _pendingProfileImage;
    private Bitmap? _profileAvatarBitmap;

    public event Action<string>? ToastRequested;
    public event Action? LibraryMetadataChanged;
    public event Action? ExportRequested;
    public event Action<AppearanceSettings>? AppearanceChanged;
    public event Action? DiscordPresenceChanged;
    public event Action? CloudProfileChanged;

    public SettingsOverlay()
    {
        InitializeComponent();
        BuildAppearanceControls();
        _appearanceSaveTimer.Tick += (_, _) => PersistPendingAppearance();
        _profileSaveTimer.Tick += (_, _) => PersistPendingProfile();
        _profileSaveAnimationTimer.Tick += (_, _) =>
        {
            var rotation = (RotateTransform)ProfileSaveLoadingIcon.RenderTransform!;
            rotation.Angle = (rotation.Angle + 18) % 360;
        };
        SearchBox.TextChanged += (_, _) => RebuildGenreVocabularyRows();
        ProfileUsernameBox.TextChanged += (_, _) =>
        {
            RefreshProfileAvatar();
            QueueProfileSave();
        };
        CloudLibrarySyncService.Current.StatusChanged += status =>
            Dispatcher.UIThread.Post(() => RefreshCloudSyncStatus(status));
        AppUpdateService.Current.StateChanged += OnAppUpdateStateChanged;
        RefreshUpdatePage(AppUpdateService.Current.State);
    }

    public void Open()
    {
        var appSettings = AppSettingsStore.Load();
        MusicAnalysisServerUrlBox.Text = appSettings.MusicAnalysisServerUrl;
        MusicAnalysisApiKeyBox.Text = appSettings.MusicAnalysisApiKey;
        _loadingDiscordPresenceSettings = true;
        DiscordPresenceToggle.IsChecked = appSettings.DiscordRichPresenceEnabled;
        UpdateDiscordPresenceToggleVisual();
        DiscordLargeImageTextBox.Text = appSettings.DiscordLargeImageText;
        _loadingDiscordPresenceSettings = false;
        _appearanceSettings = appSettings.Appearance.Clone().Clamp();
        RefreshAppearanceControls();
        RefreshAppearancePreview();
        CloudServerStatusText.IsVisible = false;
        AnalysisServerStatusText.IsVisible = false;
        FirefoxCookiesToggle.IsChecked = Values.UseFirefoxCookiesForYtDlp;
        RefreshLinuxDependencies();
        RebuildBackupDirectoryRows();
        CloudServerUrlBox.Text = appSettings.CloudServerUrl;
        LoadCloudProfile();
        SelectPage(SettingsPage.Profile);
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

    private void OnDiscordPresenceTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loadingDiscordPresenceSettings)
            return;

        AppSettingsStore.SaveDiscordPresence(
            DiscordPresenceToggle.IsChecked == true,
            null,
            DiscordLargeImageTextBox.Text);
        DiscordPresenceChanged?.Invoke();
    }

    private void OnDiscordPresenceEnabledChanged(object? sender, RoutedEventArgs e)
    {
        if (_loadingDiscordPresenceSettings)
            return;

        AppSettingsStore.SaveDiscordPresence(
            DiscordPresenceToggle.IsChecked == true,
            null,
            DiscordLargeImageTextBox.Text);
        DiscordPresenceChanged?.Invoke();
        UpdateDiscordPresenceToggleVisual();
    }

    private void UpdateDiscordPresenceToggleVisual() =>
        DiscordPresenceStatusText.Text = DiscordPresenceToggle.IsChecked == true ? "Active" : "Disabled";

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
        _selectedModelGenreId = null;
        SearchBox.Text = string.Empty;
        _genreVocabularyLoaded = true;
        _isLoading = false;
        RebuildGenreGroupRows();
        RebuildGenreVocabularyRows();
    }

    private void RebuildGenreVocabularyRows()
    {
        if (_isLoading || !_genreVocabularyLoaded) return;

        var search = SearchBox.Text?.Trim() ?? string.Empty;
        GenreVocabularyRows.Children.Clear();
        var modelGenreNames = _modelGenres.ToDictionary(genre => genre.Id, genre => genre.Name);
        var choices = _modelSubgenres
            .Where(subgenre => _selectedModelGenreId is null || subgenre.ModelGenreId == _selectedModelGenreId)
            .Where(subgenre => MatchesSearch(subgenre, modelGenreNames.GetValueOrDefault(subgenre.ModelGenreId, ""), search))
            .OrderBy(subgenre => modelGenreNames.GetValueOrDefault(subgenre.ModelGenreId, ""), StringComparer.OrdinalIgnoreCase)
            .ThenBy(subgenre => subgenre.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var subgenre in choices)
            GenreVocabularyRows.Children.Add(CreateGenreVocabularyChoice(subgenre));

        if (choices.Count == 0)
            GenreVocabularyRows.Children.Add(new TextBlock
            {
                Text = "No genres match this filter.",
                FontSize = 11,
                Opacity = 0.52,
                Margin = new Avalonia.Thickness(0, 8, 0, 4)
            });
        UpdateSummary();
    }

    private void RebuildGenreGroupRows()
    {
        GenreGroupRows.Children.Clear();
        var availableGroupIds = _modelSubgenres.Select(item => item.ModelGenreId).ToHashSet();
        if (_selectedModelGenreId is int selectedId && !availableGroupIds.Contains(selectedId))
            _selectedModelGenreId = null;

        var groups = new[] { (Id: (int?)null, Name: "All") }
            .Concat(_modelGenres
                .Where(genre => availableGroupIds.Contains(genre.Id))
                .OrderBy(genre => genre.Name, StringComparer.OrdinalIgnoreCase)
                .Select(genre => (Id: (int?)genre.Id, genre.Name)));
        foreach (var group in groups)
        {
            var selected = group.Id == _selectedModelGenreId;
            var item = new Border
            {
                Height = 28,
                Background = Brushes.Transparent,
                Padding = new Avalonia.Thickness(2, 0),
                Cursor = new Cursor(StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = group.Name,
                    FontSize = 10.5,
                    FontWeight = selected ? FontWeight.SemiBold : FontWeight.Normal,
                    Foreground = group.Id is null
                        ? ThemeResources.Brush(selected ? "Theme.Brush.Accent" : "Theme.Brush.TextSecondary")
                        : MainGenrePalette.For(group.Name),
                    Opacity = selected ? 1 : 0.72,
                    TextWrapping = TextWrapping.Wrap,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            item.PointerPressed += (_, e) =>
            {
                _selectedModelGenreId = group.Id;
                RebuildGenreGroupRows();
                RebuildGenreVocabularyRows();
                e.Handled = true;
            };
            GenreGroupRows.Children.Add(item);
        }
    }

    private bool MatchesSearch(ModelSubgenre subgenre, string modelGenreName, string search)
    {
        if (search.Length == 0) return true;
        return modelGenreName.Contains(search, StringComparison.OrdinalIgnoreCase)
               || subgenre.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private Button CreateGenreVocabularyChoice(ModelSubgenre subgenre)
    {
        var text = new TextBlock
        {
            Text = subgenre.Name,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        var button = new Button
        {
            Content = text,
            Height = 32,
            Padding = new Avalonia.Thickness(9, 4),
            CornerRadius = new Avalonia.CornerRadius(5),
            Background = Brushes.Transparent,
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            BorderThickness = new Avalonia.Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false
        };
        ToolTip.SetTip(button, CreateGenreMetadataTooltip(subgenre));
        return button;
    }

    private Control CreateGenreMetadataTooltip(ModelSubgenre subgenre)
    {
        var genreName = _modelGenres.FirstOrDefault(item => item.Id == subgenre.ModelGenreId)?.Name ?? "Model genre";
        var panel = new StackPanel { Spacing = 7 };
        panel.Children.Add(new TextBlock
        {
            Text = $"{genreName} → {subgenre.Name}",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary")
        });
        if (!string.IsNullOrWhiteSpace(subgenre.Description))
            panel.Children.Add(new TextBlock { Text = subgenre.Description, FontSize = 11, TextWrapping = TextWrapping.Wrap });
        if (!string.IsNullOrWhiteSpace(subgenre.ClassificationHint))
            panel.Children.Add(new TextBlock
            {
                Text = $"Classify when: {subgenre.ClassificationHint}",
                FontSize = 10.5,
                Foreground = ThemeResources.Brush("Theme.Brush.Accent"),
                TextWrapping = TextWrapping.Wrap
            });
        if (subgenre.BpmMin is not null || subgenre.BpmMax is not null)
            panel.Children.Add(new TextBlock
            {
                Text = subgenre.BpmMin is not null && subgenre.BpmMax is not null
                    ? $"Typical BPM: {subgenre.BpmMin}–{subgenre.BpmMax}"
                    : $"Typical BPM: {(subgenre.BpmMin is not null ? $"from {subgenre.BpmMin}" : $"up to {subgenre.BpmMax}")}",
                FontSize = 10.5,
                Opacity = 0.78
            });
        if (_distinctionsBySubgenreId.TryGetValue(subgenre.Id, out var distinctions))
        {
            panel.Children.Add(new TextBlock
            {
                Text = "Distinguish from",
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Opacity = 0.82
            });
            foreach (var distinction in distinctions)
                panel.Children.Add(new TextBlock
                {
                    Text = $"{distinction.ModelGenreName} → {distinction.ModelSubgenreName}: {distinction.Difference}",
                    FontSize = 10,
                    Opacity = 0.76,
                    TextWrapping = TextWrapping.Wrap
                });
        }
        return new Border
        {
            Background = ThemeResources.Brush("Theme.Brush.SurfaceRaised"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderStrong"),
            BorderThickness = new Avalonia.Thickness(1),
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(12, 10),
            Width = 300,
            MaxWidth = 300,
            Child = new ScrollViewer
            {
                MaxHeight = 390,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = panel
            }
        };
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

    private static Avalonia.Svg.Skia.Svg CreateSvgIcon(string path, double size) => new(new Uri("avares://Resona/"))
    {
        Path = path,
        Width = size,
        Height = size,
        Stretch = Stretch.Uniform,
        Opacity = 0.82,
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Center
    };

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

    private void ReloadGenreVocabulary()
    {
        _genreVocabularyLoaded = true;
        _isLoading = true;
        _modelGenres = MusicLibraryService.Current.GetModelGenres();
        _modelSubgenres = MusicLibraryService.Current.GetModelSubgenres();
        _distinctionsBySubgenreId = MusicLibraryService.Current.GetModelSubgenreDistinctions()
            .GroupBy(item => item.ModelSubgenreId)
            .ToDictionary(group => group.Key, group => group.ToList());
        _isLoading = false;
        CreateSubgenreOverlay.IsVisible = false;
        RebuildGenreGroupRows();
        RebuildGenreVocabularyRows();
        LibraryMetadataChanged?.Invoke();
    }

    private void OnAddSubgenreClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedModelGenreId is not int categoryId)
        {
            ToastRequested?.Invoke("Select a main genre before creating a subgenre.");
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
            "health" => SettingsPage.Health,
            "appearance" => SettingsPage.Appearance,
            "backup" => SettingsPage.Backup,
            "export" => SettingsPage.Export,
            "updates" => SettingsPage.Updates,
            "discord" => SettingsPage.Discord,
            "profile" => SettingsPage.Profile,
            "servers" => SettingsPage.Servers,
            "tags" => SettingsPage.Tags,
            "tag_rules" => SettingsPage.TagRules,
            _ => SettingsPage.GenreVocabulary
        };
        SelectPage(page);
    }

    private void SelectPage(SettingsPage page)
    {
        if (page == SettingsPage.GenreVocabulary)
            EnsureGenreVocabularyLoaded();

        _selectedPage = page;
        var isGenreVocabularyPage = page == SettingsPage.GenreVocabulary;
        var isHealthPage = page == SettingsPage.Health;
        var isBackupPage = page == SettingsPage.Backup;
        var isExportPage = page == SettingsPage.Export;
        var isUpdatesPage = page == SettingsPage.Updates;
        var isAppearancePage = page == SettingsPage.Appearance;
        var isDiscordPage = page == SettingsPage.Discord;
        var isProfilePage = page == SettingsPage.Profile;
        var isServersPage = page == SettingsPage.Servers;
        GenreVocabularyPage.IsVisible = isGenreVocabularyPage;
        HealthPage.IsVisible = isHealthPage;
        BackupPage.IsVisible = isBackupPage;
        ExportPage.IsVisible = isExportPage;
        UpdatesPage.IsVisible = isUpdatesPage;
        AppearancePage.IsVisible = isAppearancePage;
        DiscordPage.IsVisible = isDiscordPage;
        ProfilePage.IsVisible = isProfilePage;
        ServersPage.IsVisible = isServersPage;
        TagsPage.IsVisible = page == SettingsPage.Tags;
        TagRulesPage.IsVisible = page == SettingsPage.TagRules;
        GenreVocabularyNavButton.IsChecked = isGenreVocabularyPage;
        HealthNavButton.IsChecked = isHealthPage;
        BackupNavButton.IsChecked = isBackupPage;
        ExportNavButton.IsChecked = isExportPage;
        UpdatesNavButton.IsChecked = isUpdatesPage;
        AppearanceNavButton.IsChecked = isAppearancePage;
        DiscordNavButton.IsChecked = isDiscordPage;
        ProfileNavButton.IsChecked = isProfilePage;
        ServersNavButton.IsChecked = isServersPage;
        TagsNavButton.IsChecked = page == SettingsPage.Tags;
        TagRulesNavButton.IsChecked = page == SettingsPage.TagRules;

        PageTitleText.Text = page switch
        {
            SettingsPage.Health => "Health check",
            SettingsPage.Backup => "Backup",
            SettingsPage.Export => "Export",
            SettingsPage.Updates => "Updates",
            SettingsPage.Appearance => "Appearance",
            SettingsPage.Discord => "Discord presence",
            SettingsPage.Profile => "Account",
            SettingsPage.Servers => "Connections",
            SettingsPage.Tags => "Tags",
            SettingsPage.TagRules => "Tag rules",
            _ => "Genres"
        };
        PageDescriptionText.Text = isGenreVocabularyPage
            ? "Review the genre categories and subgenres used directly by the library."
            : isHealthPage
                    ? "Check database integrity, workflow consistency, channel mappings and local audio files."
                : isBackupPage
                    ? "Keep daily database snapshots in your backup locations."
                    : isExportPage
                        ? "Export the current library into a portable folder."
                        : isAppearancePage
                            ? "Tune artwork, blur, color and audio-reactive visuals. Changes are applied live."
                            : isDiscordPage
                                ? "Customize the text Discord displays while Resona is playing music."
                            : isProfilePage
                                ? "Manage your local identity and temporary session behavior."
                            : isServersPage
                                ? "Configure cloud synchronization and track analysis services."
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
        if (page == SettingsPage.Tags) ReloadTagManagement();
        if (page == SettingsPage.TagRules) ReloadTagRules();
        if (isUpdatesPage) RefreshUpdatePage(AppUpdateService.Current.State);
    }

    private void LoadCloudProfile()
    {
        try
        {
            _cloudIdentity = CloudIdentityStore.Current.GetOrCreate();
            _pendingProfileImage = _cloudIdentity.ProfileImage?.ToArray();
            _loadingProfile = true;
            ProfileUsernameBox.Text = _cloudIdentity.Username;
            _loadingProfile = false;
            ProfileUserIdText.Text = _cloudIdentity.UserId;
            ProfileStatusText.IsVisible = false;
            RefreshProfileAvatar();
            ShowProfileSavedState();
        }
        catch (Exception exception)
        {
            _loadingProfile = false;
            ProfileStatusText.Text = $"Could not load profile: {exception.Message}";
            ProfileStatusText.IsVisible = true;
            ShowProfileSaveFailedState();
        }
    }

    private void OnShowCreateSubgenreClicked(object? sender, RoutedEventArgs e)
    {
        if (_selectedModelGenreId is not int categoryId)
        {
            ToastRequested?.Invoke("Select a main genre before creating a subgenre.");
            return;
        }

        var genreName = _modelGenres.FirstOrDefault(genre => genre.Id == categoryId)?.Name ?? "main genre";
        CreateSubgenreTitleText.Text = $"Create subgenre in {genreName}";
        NewSubgenreBox.Text = string.Empty;
        CreateSubgenreOverlay.IsVisible = true;
        Dispatcher.UIThread.Post(() => NewSubgenreBox.Focus());
    }

    private void OnCancelCreateSubgenreClicked(object? sender, RoutedEventArgs e)
    {
        NewSubgenreBox.Text = string.Empty;
        CreateSubgenreOverlay.IsVisible = false;
    }

    private void OnNewSubgenreKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnAddSubgenreClicked(sender, new RoutedEventArgs());
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            OnCancelCreateSubgenreClicked(sender, new RoutedEventArgs());
        }
    }

    private async void OnChooseProfileImageClicked(object? sender, RoutedEventArgs e)
    {
        ProfileAvatarButton.Flyout?.Hide();
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose profile picture",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll]
        });
        if (files.Count == 0)
            return;

        try
        {
            await using var input = await files[0].OpenReadAsync();
            if (input.CanSeek && input.Length > 12 * 1024 * 1024)
                throw new InvalidDataException("Profile picture must be smaller than 12 MB.");
            using var buffer = new MemoryStream();
            await input.CopyToAsync(buffer);
            _pendingProfileImage = ThumbnailService.CreateSquareArtwork(buffer.ToArray(), 256, 88)
                ?? throw new InvalidDataException("The selected image could not be decoded.");
            RefreshProfileAvatar();
            ProfileAvatarButton.Flyout?.Hide();
            PersistProfile();
        }
        catch (Exception exception)
        {
            ProfileStatusText.Text = $"Could not use picture: {exception.Message}";
            ProfileStatusText.IsVisible = true;
        }
    }

    private void OnRemoveProfileImageClicked(object? sender, RoutedEventArgs e)
    {
        _pendingProfileImage = null;
        RefreshProfileAvatar();
        ProfileAvatarButton.Flyout?.Hide();
        PersistProfile();
    }

    private void RefreshProfileAvatar()
    {
        _profileAvatarBitmap?.Dispose();
        _profileAvatarBitmap = null;
        if (_pendingProfileImage is { Length: > 0 })
        {
            using var stream = new MemoryStream(_pendingProfileImage, writable: false);
            _profileAvatarBitmap = new Bitmap(stream);
        }

        ProfileAvatarImage.Source = _profileAvatarBitmap;
        ProfileAvatarImage.IsVisible = _profileAvatarBitmap is not null;
        ProfileAvatarPlaceholderText.IsVisible = _profileAvatarBitmap is null;
        var username = ProfileUsernameBox.Text?.Trim();
        ProfileAvatarPlaceholderText.Text = string.IsNullOrWhiteSpace(username)
            ? "?"
            : username[..1].ToUpperInvariant();
        RemoveProfileImageMenuButton.IsVisible = _pendingProfileImage is { Length: > 0 };
    }

    private void QueueProfileSave()
    {
        if (_loadingProfile)
            return;

        _profileSavePending = true;
        _profileSaveTimer.Stop();
        _profileSaveTimer.Start();
        ProfileStatusText.IsVisible = false;
        ProfileSaveCheckIcon.IsVisible = false;
        ProfileSaveLoadingIcon.IsVisible = true;
        _profileSaveAnimationTimer.Start();
    }

    private void PersistPendingProfile()
    {
        _profileSaveTimer.Stop();
        if (!_profileSavePending)
            return;

        PersistProfile();
    }

    private void PersistProfile()
    {
        _profileSaveTimer.Stop();
        _profileSavePending = false;
        var username = ProfileUsernameBox.Text?.Trim() ?? string.Empty;

        try
        {
            _cloudIdentity = CloudIdentityStore.Current.UpdateProfile(
                username,
                _cloudIdentity?.Bio,
                _pendingProfileImage);
            _loadingProfile = true;
            ProfileUsernameBox.Text = _cloudIdentity.Username;
            _loadingProfile = false;
            ProfileStatusText.IsVisible = false;
            ShowProfileSavedState();
            CloudProfileChanged?.Invoke();
        }
        catch (Exception exception)
        {
            _loadingProfile = false;
            ProfileStatusText.Text = $"Could not save profile: {exception.Message}";
            ProfileStatusText.IsVisible = true;
            ShowProfileSaveFailedState();
        }
    }

    private void ShowProfileSavedState()
    {
        _profileSaveAnimationTimer.Stop();
        ((RotateTransform)ProfileSaveLoadingIcon.RenderTransform!).Angle = 0;
        ProfileSaveLoadingIcon.IsVisible = false;
        ProfileSaveCheckIcon.IsVisible = true;
    }

    private void ShowProfileSaveFailedState()
    {
        _profileSaveAnimationTimer.Stop();
        ((RotateTransform)ProfileSaveLoadingIcon.RenderTransform!).Angle = 0;
        ProfileSaveLoadingIcon.IsVisible = false;
        ProfileSaveCheckIcon.IsVisible = false;
    }

    private async void OnCopyProfileUserIdClicked(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null || _cloudIdentity is null)
            return;
        await clipboard.SetTextAsync(_cloudIdentity.UserId);
        ToastRequested?.Invoke("User ID copied");
    }

    private async void OnSynchronizeCloudClicked(object? sender, RoutedEventArgs e)
    {
        AppSettingsStore.SaveCloudServerUrl(CloudServerUrlBox.Text);
        var status = await CloudLibrarySyncService.Current.SynchronizeAsync();
        RefreshCloudSyncStatus(status);
    }

    private void RefreshCloudSyncStatus(CloudSyncStatus status)
    {
        CloudServerStatusText.Text = status.Message;
        CloudServerStatusText.IsVisible = true;
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

        AppSettingsStore.SaveMusicAnalysisServerConfiguration(serverUrl, MusicAnalysisApiKeyBox.Text);
        BackgroundAnalysisService.Current.NotifyServerConfigurationChanged();
        MusicAnalysisServerUrlBox.Text = serverUrl;
        ShowAnalysisServerStatus("Configuration saved", isSuccess: true);
        ToastRequested?.Invoke("Analysis server configuration saved.");
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
            using var service = new TrackAnalysisService(
                serverUrlProvider: () => serverUrl,
                apiKeyProvider: () => MusicAnalysisApiKeyBox.Text);
            var isHealthy = await service.CheckHealthAsync();
            if (isHealthy)
            {
                AppSettingsStore.SaveMusicAnalysisServerConfiguration(serverUrl, MusicAnalysisApiKeyBox.Text);
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
        _tagUsageCounts = MusicLibraryService.Current.GetAllTrackTagIds()
            .Values
            .SelectMany(tagIds => tagIds)
            .GroupBy(tagId => tagId)
            .ToDictionary(group => group.Key, group => group.Count());
        CreateTagOverlay.IsVisible = false;
        NewTagBox.Text = string.Empty;
        TagVocabularyHintText.Text = _tags.Count == 1 ? "1 tag" : $"{_tags.Count} tags";
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
            CornerRadius = new Avalonia.CornerRadius(7)
        };
        var panel = new StackPanel();
        var current = tag;
        StackPanel? editorPanel = null;

        var header = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            Padding = new Avalonia.Thickness(12, 9),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        var headerContent = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,*") };
        var titleText = new TextBlock
        {
            Text = tag.Name,
            FontSize = 13.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemeResources.Brush("Theme.Brush.TextPrimary"),
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 1);
        headerContent.Children.Add(titleText);
        var usageCount = _tagUsageCounts.GetValueOrDefault(tag.Id);
        var usageText = new TextBlock
        {
            Text = usageCount == 1 ? "1 track" : $"{usageCount} tracks",
            FontSize = 10,
            Opacity = 0.5,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(usageText, 2);
        headerContent.Children.Add(usageText);
        header.Content = headerContent;
        panel.Children.Add(header);

        header.Click += (_, _) =>
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
            row.Background = ThemeResources.Brush(editorPanel.IsVisible
                ? "Theme.Brush.SurfaceSelected"
                : "Theme.Brush.SurfaceTranslucent");
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
            Margin = new Avalonia.Thickness(12, 0, 12, 12)
        };

        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
        fields.Children.Add(CreateLabeledField("Tag", nameBox));
        var save = new Button
        {
            Content = CreateSvgIcon("/Assets/save.svg", 15),
            Width = 30,
            Height = 30,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        ToolTip.SetTip(save, "Save tag");
        var remove = new Button
        {
            Content = CreateSvgIcon("/Assets/trash.svg", 15),
            Width = 30,
            Height = 30,
            Padding = new Avalonia.Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Avalonia.Thickness(0),
            VerticalAlignment = VerticalAlignment.Bottom,
            Opacity = .7
        };
        ToolTip.SetTip(remove, "Delete tag");
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
        if (name.Length == 0)
        {
            ToastRequested?.Invoke("Tag name is required.");
            return;
        }
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

    private void OnShowCreateTagClicked(object? sender, RoutedEventArgs e)
    {
        NewTagBox.Text = string.Empty;
        CreateTagOverlay.IsVisible = true;
        Dispatcher.UIThread.Post(() => NewTagBox.Focus());
    }

    private void OnCancelCreateTagClicked(object? sender, RoutedEventArgs e)
    {
        NewTagBox.Text = string.Empty;
        CreateTagOverlay.IsVisible = false;
    }

    private void OnNewTagKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnAddTagClicked(sender, new RoutedEventArgs());
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            OnCancelCreateTagClicked(sender, new RoutedEventArgs());
        }
    }

    private void UpdateSummary()
    {
        if (_selectedPage != SettingsPage.GenreVocabulary) return;
        SummaryText.Text = BuildSummaryText();
    }

    private string BuildSummaryText()
    {
        var relevantSubgenres = _selectedModelGenreId is int modelGenreId
            ? _modelSubgenres.Where(subgenre => subgenre.ModelGenreId == modelGenreId).ToList()
            : _modelSubgenres;
        var scope = _selectedModelGenreId is int selectedId
            ? _modelGenres.FirstOrDefault(genre => genre.Id == selectedId)?.Name ?? "Selected genre"
            : "All categories";
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
        else
        {
            // These controls share the player's Bitmap instance. Clear every
            // reference before MusicView retires that bitmap.
            PreviewLibraryArtwork.Source = null;
            PreviewTrackArtworkBlur.Source = null;
            PreviewCoverHalo.Source = null;
            PreviewCoverArtwork.Source = null;
            PreviewPlayerArtwork.Source = null;
        }
        RefreshAppearancePreview();
    }

    private async void OnRunHealthCheckClicked(object? sender, RoutedEventArgs e)
    {
        if (!RunHealthCheckButton.IsEnabled)
            return;

        RunHealthCheckButton.IsEnabled = false;
        RunHealthCheckButton.Content = "Checking…";
        HealthResultPanel.IsVisible = true;
        HealthStatusDot.Background = new SolidColorBrush(Color.Parse("#7895AE"));
        HealthSummaryText.Text = "Checking database and files…";
        HealthCountsText.Text = "This can take a moment for a large library.";
        HealthLastRunText.Text = string.Empty;
        HealthIssueRows.Children.Clear();
        try
        {
            var report = await DatabaseHealthService.Current.CheckAsync();
            RenderHealthReport(report);
        }
        catch (Exception exception)
        {
            WorkflowLog.Error("database-health", "Database health check failed.", exception);
            HealthStatusDot.Background = new SolidColorBrush(Color.Parse("#E87878"));
            HealthSummaryText.Text = "Health check could not finish";
            HealthCountsText.Text = exception.Message;
            HealthLastRunText.Text = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        }
        finally
        {
            RunHealthCheckButton.IsEnabled = true;
            RunHealthCheckButton.Content = "Run health check";
        }
    }

    private void RenderHealthReport(DatabaseHealthReport report)
    {
        HealthIssueRows.Children.Clear();
        var statusColor = report.ErrorCount > 0
            ? "#E87878"
            : report.WarningCount > 0 ? "#E6BF55" : "#79C994";
        HealthStatusDot.Background = new SolidColorBrush(Color.Parse(statusColor));
        HealthSummaryText.Text = report.IsHealthy
            ? "No health issues found"
            : $"{report.ErrorCount} error{(report.ErrorCount == 1 ? string.Empty : "s")} · "
              + $"{report.WarningCount} warning{(report.WarningCount == 1 ? string.Empty : "s")}";
        HealthCountsText.Text = $"{report.TrackCount:N0} database tracks · "
                                + $"{report.ReferencedFileCount:N0} referenced filenames · "
                                + $"{report.AudioFileCount:N0} audio files on disk";
        HealthLastRunText.Text = $"Finished {report.FinishedAtUtc.ToLocalTime():g} · read-only";

        foreach (var issue in report.Issues)
            HealthIssueRows.Children.Add(CreateHealthIssueRow(issue));
    }

    private static Control CreateHealthIssueRow(DatabaseHealthIssue issue)
    {
        var color = issue.Severity switch
        {
            DatabaseHealthSeverity.Error => Color.Parse("#E87878"),
            DatabaseHealthSeverity.Warning => Color.Parse("#E6BF55"),
            _ => Color.Parse("#78AEE8")
        };
        var row = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x18, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x4A, color.R, color.G, color.B)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(11, 9)
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 10
        };
        grid.Children.Add(new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(color),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 5, 0, 0)
        });

        var content = new StackPanel { Spacing = 3 };
        content.Children.Add(new TextBlock
        {
            Text = issue.Title,
            FontSize = 11.5,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(new TextBlock
        {
            Text = $"{issue.Category} · {issue.Detail}",
            FontSize = 10.5,
            Opacity = 0.68,
            TextWrapping = TextWrapping.Wrap
        });
        if (issue.Examples is { Count: > 0 })
        {
            content.Children.Add(new TextBlock
            {
                Text = string.Join(Environment.NewLine, issue.Examples),
                FontSize = 9.5,
                Foreground = new SolidColorBrush(Color.Parse("#9EABB7")),
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 5
            });
        }
        Grid.SetColumn(content, 1);
        grid.Children.Add(content);

        var count = new TextBlock
        {
            Text = issue.Count > 1 ? issue.Count.ToString("N0", CultureInfo.CurrentCulture) : string.Empty,
            FontSize = 10,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(color),
            Margin = new Thickness(6, 1, 0, 0)
        };
        Grid.SetColumn(count, 2);
        grid.Children.Add(count);
        row.Child = grid;
        return row;
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
        PreviewSpectrumVisualizer.Opacity = _appearanceSettings.SpectrumVisualizerIntensity / 100d;
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
        PreviewTrackWash.PrimaryColor = _previewPrimary;
        PreviewTrackWash.SecondaryColor = _previewSecondary;
        PreviewTrackWash.Strength = _appearanceSettings.TrackColorWashStrength / 100d;
        PreviewTrackWash.Reach = _appearanceSettings.TrackColorWashReach;

        PreviewPlayerDarkening.Background = new SolidColorBrush(PreviewWithOpacity(
            Color.Parse("#242424"), _appearanceSettings.PlayerBackgroundDarkening / 100d));
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
        AddAppearanceSlider(PlayerAppearanceRows, "Artwork fade duration", "How long the cover transition takes when the active song changes. Set to 0 for an instant change.",
            0, 30, settings => settings.ArtworkFadeDuration,
            (settings, value) => settings.ArtworkFadeDuration = value, SecondsValue);
        AddAppearanceSlider(PlayerAppearanceRows, "Song fade duration", "Crossfade duration when playback advances automatically. Set to 0 to disable crossfading.",
            0, 30, settings => settings.SongFadeDuration,
            (settings, value) => settings.SongFadeDuration = value, SecondsValue);
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
        AddAppearanceSlider(AudioAppearanceRows, "Visualizer intensity", "Opacity and visible color strength of the frequency visualizer.",
            0, 100, settings => settings.SpectrumVisualizerIntensity,
            (settings, value) => settings.SpectrumVisualizerIntensity = value, PercentValue);
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
        slider.Classes.Add("appearance-simple");
        ToolTip.SetTip(slider, "Double-click to restore the default value.");
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

        var controlRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,52"),
            ColumnSpacing = 10
        };
        controlRow.Children.Add(slider);
        Grid.SetColumn(valueText, 1);
        controlRow.Children.Add(valueText);

        var row = new StackPanel { Spacing = 5 };
        row.Children.Add(labels);
        row.Children.Add(controlRow);
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
    private static string SecondsValue(double value) => $"{value:0} s";

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        PersistPendingAppearance();
        PersistPendingProfile();
        IsVisible = false;
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

    private enum SettingsPage { GenreVocabulary, Health, Appearance, Discord, Profile, Servers, Backup, Export, Updates, Tags, TagRules }
}
