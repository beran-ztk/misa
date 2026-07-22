using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class EditTrackOverlay : UserControl
{
    private MusicTrack? _track;
    private List<Tag> _tags = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    private readonly List<(Tag Tag, ToggleButton Btn)> _tagChips = [];
    private readonly List<(Style Style, ToggleButton Btn)> _styleChips = [];

    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private HashSet<int> _modelGenreIds = [];
    private Dictionary<int, ModelSubgenre> _modelSubgenresById = [];
    private Dictionary<int, string> _modelGenreNamesById = [];
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private HashSet<int> _visibleDetectedModelGenreIds = [];
    private int? _modelGenreFilterId;
    private string _modelGenreSearchText = string.Empty;
    private bool _buildingModelGenreChoices;
    private readonly Dictionary<string, string?> _pendingAttributeOverrides = [];
    private bool _buildingSoundProfile;
    private readonly DispatcherTimer _analysisElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _analysisStartedAt;
    private bool _isPlayingPreview;
    private bool _loadingTrack;
    private bool _isPublic;
    private bool _initialIsPublic;
    private string _initialTitle = string.Empty;
    private bool _isEditingInformation;
    private int? _initialRatingId;
    private HashSet<int> _initialTagIds = [];
    private HashSet<int> _initialStyleIds = [];
    private HashSet<int> _initialEnabledModelGenreIds = [];
    private HashSet<int> _pendingEnabledModelGenreIds = [];
    private Dictionary<string, string?> _initialAttributeOverrides = [];

    public event Action<int>? TrackSaved;
    public event Action<MusicTrack>? PreviewRequested;
    public event Action? PreviewClosed;
    public event Action<string>? ToastRequested;

    public EditTrackOverlay()
    {
        InitializeComponent();
        TitleBox.TextChanged += (_, _) => UpdateSaveButton();
        RatingBox.SelectionChanged += (_, _) =>
        {
            UpdateRatingVisual();
            UpdateSaveButton();
        };
        _analysisElapsedTimer.Tick += (_, _) => UpdateAnalysisElapsedTime();
    }

    public void Open(MusicTrack track, bool analyzeAfterOpening = false)
    {
        _track = track;
        _isPlayingPreview = false;
        LoadLookups();
        Prefill(track);
        IsVisible = true;
        if (analyzeAfterOpening)
            _ = AnalyzeImportedTrackAsync(track);
    }

    private void LoadLookups()
    {
        _tags = MusicLibraryService.Current.GetTags();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        StylesSection.IsVisible = _styles.Count > 0;

        RatingBox.ItemsSource = _ratings.Select(r => r.Name).ToList();
    }

    private void Prefill(MusicTrack track)
    {
        _loadingTrack = true;
        UnsavedChangesLayer.IsVisible = false;
        _pendingAttributeOverrides.Clear();
        TitleBox.Text = track.Title;
        SetInformationEditing(false);
        UpdateInformationDisplay(track);
        SetPublicSelection(track.IsPublic);

        var ratingIndex = _ratings.FindIndex(r => r.Id == track.RatingId);
        RatingBox.SelectedIndex = ratingIndex;
        UpdateRatingVisual();

        var selectedTagIds = MusicLibraryService.Current.GetTrackTagIds(track.Id).ToHashSet();
        var selectedStyleIds = MusicLibraryService.Current.GetTrackStyleIds(track.Id).ToHashSet();
        var selectedModelGenreIds = ResetModelGenreSelectionFromDatabase(track);

        ShowModelSelectedGenres(track);
        ShowDetectedGenres(track);
        RebuildTagChips(selectedTagIds);
        RebuildStyleChips(selectedStyleIds);
        ShowAudioAnalysis(track);
        ShowSoundProfile(track);
        ShowUsageStats(track);
        CaptureChangeSnapshot(track, selectedTagIds, selectedStyleIds, selectedModelGenreIds);
        _loadingTrack = false;
        UpdateSaveButton();
    }

    public void RefreshUsageStats()
    {
        if (_track is not null)
            ShowUsageStats(_track);
    }

    private void ShowUsageStats(MusicTrack track)
    {
        var usage = MusicLibraryService.Current.GetTrackUsageStats(track.Id);
        var listened = usage.ListenedSeconds >= 60
            ? $"{usage.ListenedSeconds / 60} min listened"
            : $"{usage.ListenedSeconds} sec listened";
        TrackUsageText.Text = $"{usage.PlayCount} plays  ·  {listened}  ·  {usage.SkipCount} skips";
    }

    private void OnEditInformationClicked(object? sender, RoutedEventArgs e)
    {
        if (_isEditingInformation && _track is not null)
            UpdateInformationDisplay(_track);
        SetInformationEditing(!_isEditingInformation);
    }

    private void SetInformationEditing(bool isEditing)
    {
        _isEditingInformation = isEditing;
        TitleBox.IsVisible = isEditing;
        TitleDisplayText.IsVisible = !isEditing;
        EditInformationButton.Background = isEditing ? Brush("#173C54") : Brushes.Transparent;
        ToolTip.SetTip(EditInformationButton, isEditing ? "Finish editing title" : "Edit title");
        if (isEditing)
            TitleBox.Focus();
    }

    private void UpdateInformationDisplay(MusicTrack track)
    {
        TitleDisplayText.Text = DisplayValue(TitleBox.Text);
        ChannelDisplayText.Text = DisplayValue(track.ChannelName);
        YouTubeUrlDisplayText.Text = DisplayValue(track.CanonicalUrl);
        ChannelUrlDisplayText.Text = DisplayValue(track.ChannelUrl);
        CopyYouTubeUrlButton.IsEnabled = !string.IsNullOrWhiteSpace(track.CanonicalUrl);
        CopyChannelUrlButton.IsEnabled = !string.IsNullOrWhiteSpace(track.ChannelUrl);
        ToolTip.SetTip(TitleDisplayText, TitleBox.Text);
        ToolTip.SetTip(ChannelDisplayText, track.ChannelName);
        ToolTip.SetTip(YouTubeUrlDisplayText, track.CanonicalUrl);
        ToolTip.SetTip(ChannelUrlDisplayText, track.ChannelUrl);
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private async void OnCopyYouTubeUrlClicked(object? sender, RoutedEventArgs e) =>
        await CopyUrlAsync(_track?.CanonicalUrl, "YouTube URL");

    private async void OnCopyChannelUrlClicked(object? sender, RoutedEventArgs e) =>
        await CopyUrlAsync(_track?.ChannelUrl, "Channel URL");

    private async System.Threading.Tasks.Task CopyUrlAsync(string? url, string label)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            ToastRequested?.Invoke($"No {label.ToLowerInvariant()} available");
            return;
        }

        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            ToastRequested?.Invoke("Clipboard is not available");
            return;
        }

        await clipboard.SetTextAsync(url.Trim());
        ToastRequested?.Invoke($"{label} copied");
    }

    private void RebuildTagChips(IReadOnlySet<int> selectedTagIds)
    {
        TagsPanel.Children.Clear();
        _tagChips.Clear();

        var chips = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
        foreach (var tag in _tags
                     .OrderByDescending(tag => selectedTagIds.Contains(tag.Id))
                     .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase))
        {
            var isSelected = selectedTagIds.Contains(tag.Id);
            var btn = CreateTagButton(tag, isSelected);
            btn.IsCheckedChanged += (_, _) =>
            {
                ApplyTagVisual(btn, tag);
                UpdateTagSummary();
                UpdateSaveButton();
            };

            _tagChips.Add((tag, btn));
            chips.Children.Add(btn);
        }

        TagsPanel.Children.Add(chips);
        UpdateTagSummary();
    }

    private static ToggleButton CreateTagButton(Tag tag, bool isSelected)
    {
        var label = new TextBlock
        {
            Text = tag.Name,
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var button = new ToggleButton
        {
            Content = label,
            IsChecked = isSelected,
            Width = 120,
            Height = 29,
            Margin = new Avalonia.Thickness(0, 0, 6, 6),
            Padding = new Avalonia.Thickness(8, 2),
            CornerRadius = new Avalonia.CornerRadius(3),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Tag = label
        };
        ApplyTagVisual(button, tag);
        return button;
    }

    private static void ApplyTagVisual(ToggleButton button, Tag tag)
    {
        var selected = button.IsChecked == true;
        var accent = SafeBrush(null, "#C7D2AD");
        button.Background = new SolidColorBrush(Color.Parse(selected ? "#1A3140" : "#1A2026"));
        button.BorderBrush = selected ? accent : new SolidColorBrush(Color.Parse("#394653"));
        button.BorderThickness = new Avalonia.Thickness(1);
        if (button.Tag is TextBlock label)
        {
            label.Foreground = selected
                ? accent
                : new SolidColorBrush(Color.Parse("#D7E0E8"));
        }
    }

    private static IBrush SafeBrush(string? color, string fallback)
    {
        try { return new SolidColorBrush(Color.Parse(string.IsNullOrWhiteSpace(color) ? fallback : color)); }
        catch { return new SolidColorBrush(Color.Parse(fallback)); }
    }

    private void UpdateTagSummary()
    {
        var selectedCount = _tagChips.Count(item => item.Btn.IsChecked == true);
        TagSummaryText.Text = selectedCount == 0
            ? "No tags"
            : $"{selectedCount} selected";
    }

    private void ShowTagSuggestions(MusicTrack track)
    {
        SuggestedTagsSection.IsVisible = false;
        SuggestedTagsPanel.Children.Clear();
    }

    private void RebuildStyleChips(IReadOnlySet<int>? selectedStyleIds = null)
    {
        var styleCounts = MetadataCountService.StyleCountsForGenres(new Dictionary<int, List<int>>(), _allTrackStyleIds, new HashSet<int>());

        selectedStyleIds ??= _styleChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Style.Id)
            .ToHashSet();

        var sorted = _styles
            .OrderByDescending(s => selectedStyleIds.Contains(s.Id))
            .ThenByDescending(s => styleCounts.GetValueOrDefault(s.Id, 0))
            .ThenBy(s => s.Name)
            .ToList();

        StylesPanel.Children.Clear();
        _styleChips.Clear();

        foreach (var style in sorted)
        {
            var count = styleCounts.GetValueOrDefault(style.Id, 0);
            var selected = selectedStyleIds.Contains(style.Id);
            var btn = MetadataChipFactory.Create(style.Name, count, selected);
            btn.Opacity = count > 0 || selected ? 1.0 : 0.48;
            btn.IsCheckedChanged += (_, _) => UpdateSaveButton();
            _styleChips.Add((style, btn));
            StylesPanel.Children.Add(btn);
        }
    }

    private HashSet<int> SelectedTagIds() =>
        _tagChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Tag.Id)
            .ToHashSet();

    private HashSet<int> SelectedStyleIds() =>
        _styleChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Style.Id)
            .ToHashSet();

    private int? SelectedRatingId() =>
        RatingBox.SelectedIndex >= 0 && RatingBox.SelectedIndex < _ratings.Count
            ? _ratings[RatingBox.SelectedIndex].Id
            : null;

    private void CaptureChangeSnapshot(
        MusicTrack track,
        IReadOnlySet<int> selectedTagIds,
        IReadOnlySet<int> selectedStyleIds,
        IReadOnlySet<int> selectedModelGenreIds)
    {
        _initialTitle = track.Title;
        _initialRatingId = track.RatingId;
        _initialIsPublic = track.IsPublic;
        _initialTagIds = selectedTagIds.ToHashSet();
        _initialStyleIds = selectedStyleIds.ToHashSet();
        _initialEnabledModelGenreIds = selectedModelGenreIds.ToHashSet();
        _pendingEnabledModelGenreIds = selectedModelGenreIds.ToHashSet();
        _initialAttributeOverrides = MusicLibraryService.Current.GetTrackDerivedAttributes(track.Id)
            .ToDictionary(attribute => attribute.Key, attribute => attribute.ManualValue);
    }

    private HashSet<int> ResetModelGenreSelectionFromDatabase(MusicTrack track)
    {
        var selectedModelGenreIds = MusicLibraryService.Current.GetTrackModelGenres(track.Id)
            .Where(assignment => assignment.IsEnabled)
            .Select(assignment => assignment.GenreId)
            .ToHashSet();
        _initialEnabledModelGenreIds = selectedModelGenreIds.ToHashSet();
        _pendingEnabledModelGenreIds = selectedModelGenreIds.ToHashSet();
        return selectedModelGenreIds;
    }

    private bool HasUnsavedChanges()
    {
        if (_track is null || _loadingTrack)
            return false;

        if (!string.Equals(TitleBox.Text?.Trim() ?? string.Empty, _initialTitle, StringComparison.Ordinal))
            return true;
        if (SelectedRatingId() != _initialRatingId)
            return true;
        if (_isPublic != _initialIsPublic)
            return true;
        if (!SelectedTagIds().SetEquals(_initialTagIds))
            return true;
        if (!SelectedStyleIds().SetEquals(_initialStyleIds))
            return true;
        if (!_pendingEnabledModelGenreIds.SetEquals(_initialEnabledModelGenreIds))
            return true;

        foreach (var key in _initialAttributeOverrides.Keys.Concat(_pendingAttributeOverrides.Keys).Distinct())
        {
            _initialAttributeOverrides.TryGetValue(key, out var initialValue);
            var currentValue = _pendingAttributeOverrides.TryGetValue(key, out var pendingValue)
                ? pendingValue
                : initialValue;
            if (!string.Equals(currentValue, initialValue, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void UpdateSaveButton()
    {
        var canSave = _track != null && !string.IsNullOrWhiteSpace(TitleBox.Text);
        var hasUnsavedChanges = HasUnsavedChanges();
        SaveBtn.IsEnabled = canSave;
        UnsavedChangesBadge.IsVisible = hasUnsavedChanges;
    }

    private void UpdateRatingVisual()
    {
        var name = RatingBox.SelectedIndex >= 0 && RatingBox.SelectedIndex < _ratings.Count
            ? _ratings[RatingBox.SelectedIndex].Name
            : "None";
        var (_, _, foreground) = RatingColors(name);
        RatingBox.Background = Brush("#161C22");
        RatingBox.BorderBrush = Brush("#3B4955");
        RatingBox.Foreground = foreground;
    }

    private static (IBrush Background, IBrush Border, IBrush Foreground) RatingColors(string name) => name switch
    {
        "Favorite" => (Brush("#3B341C"), Brush("#E7BE4B"), Brush("#FFE39A")),
        "Great" => (Brush("#183827"), Brush("#4EC27A"), Brush("#B7F2CC")),
        "Good" => (Brush("#17354B"), Brush("#4DA5DD"), Brush("#BCE7FF")),
        "Okay" => (Brush("#2C3440"), Brush("#8795A7"), Brush("#DAE1E9")),
        RatingNames.Avoid => (Brush("#402326"), Brush("#DD6A70"), Brush("#FFC1C4")),
        _ => (Brush("#20252B"), Brush("#4C5967"), Brush("#BBC5CE"))
    };

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private void OnPublicVisibilityPressed(object? sender, PointerPressedEventArgs e)
    {
        SetPublicSelection(true);
        e.Handled = true;
    }

    private void OnPrivateVisibilityPressed(object? sender, PointerPressedEventArgs e)
    {
        SetPublicSelection(false);
        e.Handled = true;
    }

    private void SetPublicSelection(bool isPublic)
    {
        _isPublic = isPublic;
        if (VisibilitySelectionIndicator.RenderTransform is TranslateTransform indicatorTransform)
            indicatorTransform.X = isPublic ? 0 : 93;
        VisibilitySelectionIndicator.CornerRadius = isPublic
            ? new CornerRadius(6, 0, 0, 6)
            : new CornerRadius(0, 6, 6, 0);
        PublicVisibilityText.Foreground = Brush(isPublic ? "#FFFFFF" : "#B8C5CE");
        PrivateVisibilityText.Foreground = Brush(isPublic ? "#B8C5CE" : "#FFFFFF");
        UpdateSaveButton();
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var trackId = _track?.Id;
        if (SaveCurrentChanges(closeAfterSave: true))
            TrackSaved?.Invoke(trackId!.Value);
    }

    private bool SaveCurrentChanges(bool closeAfterSave)
    {
        if (_track == null || string.IsNullOrWhiteSpace(TitleBox.Text))
            return false;

        var styleIds = SelectedStyleIds().ToList();

        MusicLibraryService.Current.UpdateTrack(
            _track.Id,
            TitleBox.Text!.Trim(),
            [],
            SelectedRatingId(),
            styleIds,
            _isPublic);

        MusicLibraryService.Current.SetTrackManualTags(_track.Id, SelectedTagIds());

        foreach (var genreId in _initialEnabledModelGenreIds
                     .Concat(_pendingEnabledModelGenreIds)
                     .Distinct())
        {
            MusicLibraryService.Current.SetTrackModelGenreEnabled(
                _track.Id,
                genreId,
                _pendingEnabledModelGenreIds.Contains(genreId));
        }

        foreach (var overrideValue in _pendingAttributeOverrides)
            MusicLibraryService.Current.SetTrackDerivedAttributeOverride(_track.Id, overrideValue.Key, overrideValue.Value);

        _track = _track with
        {
            Title = TitleBox.Text!.Trim(),
            RatingId = SelectedRatingId(),
            IsPublic = _isPublic
        };
        CaptureChangeSnapshot(_track, SelectedTagIds(), styleIds.ToHashSet(), _pendingEnabledModelGenreIds);
        _pendingAttributeOverrides.Clear();
        UpdateSaveButton();
        if (closeAfterSave)
            CloseOverlay(skipUnsavedCheck: true);
        return true;
    }

    private void OnPreviewClicked(object? sender, RoutedEventArgs e)
    {
        if (_track is null) return;
        _isPlayingPreview = true;
        PreviewRequested?.Invoke(_track);
    }

    private async System.Threading.Tasks.Task AnalyzeImportedTrackAsync(MusicTrack track)
    {
        AnalysisBusyLayer.IsVisible = true;
        _analysisStartedAt = DateTime.UtcNow;
        AnalysisElapsedText.Text = "0:00";
        var filePath = System.IO.Path.Combine(Values.TracksDirectory, track.FileName);
        var fileSize = System.IO.File.Exists(filePath) ? new System.IO.FileInfo(filePath).Length : 0;
        var estimate = MusicLibraryService.Current.EstimateAnalysisDuration(track.DurationSeconds, fileSize);
        AnalysisEstimateText.Text = estimate is null
            ? "Building an estimate from completed analyses."
            : $"Typical time for similar tracks: about {FormatEstimate(estimate.Value)}";
        _analysisElapsedTimer.Start();
        SaveBtn.IsEnabled = false;
        var error = await MusicLibraryService.Current.AnalyzeTrackAsync(track);
        _analysisElapsedTimer.Stop();
        AnalysisBusyLayer.IsVisible = false;
        ResetModelGenreSelectionFromDatabase(track);
        ShowModelSelectedGenres(track);
        ShowDetectedGenres(track);
        ShowAudioAnalysis(track);
        ShowSoundProfile(track);
        UpdateSaveButton();
    }

    private static string FormatEstimate(TimeSpan duration) => duration.TotalMinutes >= 1
        ? $"{Math.Ceiling(duration.TotalMinutes):0} min"
        : $"{Math.Max(1, Math.Round(duration.TotalSeconds)):0} sec";

    private void ShowAudioAnalysis(MusicTrack track)
    {
        var analysis = MusicLibraryService.Current.GetTrackAudioAnalysis(track.Id);
        AudioAnalysisSection.IsVisible = analysis is not null;
        AnalysisCharacterSection.IsVisible = AudioAnalysisSection.IsVisible || SoundProfileSection.IsVisible;
        if (analysis is null) return;

        BpmText.Text = analysis.Bpm is double bpm ? $"{bpm:0.#} BPM" : "—";
        BpmText.Foreground = analysis.Bpm is double bpmForColor
            ? AnalysisColorScale.Tempo(bpmForColor)
            : Brushes.White;
        IntegratedLoudnessText.Text = analysis.IntegratedLoudness is double loudness
            ? $"{loudness:0.#} LUFS"
            : "—";
        IntegratedLoudnessText.Foreground = analysis.IntegratedLoudness is double loudnessForColor
            ? AnalysisColorScale.IntegratedLoudness(loudnessForColor)
            : Brushes.White;
        LoudnessRangeText.Text = analysis.LoudnessRange is double range
            ? $"{range:0.#} LU"
            : "—";
        LoudnessRangeText.Foreground = analysis.LoudnessRange is double detectedRange
            ? AnalysisColorScale.LoudnessRange(detectedRange)
            : Brushes.White;

        var tempoInsight = analysis.Bpm is double detectedBpm
            ? GetTempoInsight(detectedBpm)
            : "Tempo could not be determined.";
        var loudnessInsight = analysis.IntegratedLoudness is double detectedLoudness
            ? GetIntegratedLoudnessInsight(detectedLoudness)
            : "Average loudness could not be determined.";
        var dynamicsInsight = analysis.LoudnessRange is double loudnessRange
            ? GetLoudnessRangeInsight(loudnessRange)
            : "Loudness variation could not be determined.";
        ToolTip.SetTip(BpmMetricCard, tempoInsight);
        ToolTip.SetTip(LoudnessMetricCard, loudnessInsight);
        ToolTip.SetTip(DynamicsMetricCard, dynamicsInsight);
    }

    private void ShowSoundProfile(MusicTrack track)
    {
        var models = MusicLibraryService.Current.GetExperimentalAnalysis(track.Id);
        var derived = MusicLibraryService.Current.GetTrackDerivedAttributes(track.Id);
        SoundProfileSection.IsVisible = models.Count > 0 || derived.Count > 0;
        AnalysisCharacterSection.IsVisible = AudioAnalysisSection.IsVisible || SoundProfileSection.IsVisible;
        SoundProfileLeftPanel.Children.Clear();
        SoundProfileRightPanel.Children.Clear();
        MirexCharacterPanel.Children.Clear();
        MoodSignalsPanel.Children.Clear();
        if (!SoundProfileSection.IsVisible) return;

        _buildingSoundProfile = true;
        foreach (var attribute in derived)
            AddDerivedAttribute(attribute);
        _buildingSoundProfile = false;

        AddMirexCharacter(models);

        AddSignal("Happy", "How strongly the model detects a happy mood.", Signal(models, "mood happy", "happy"));
        AddSignal("Sad", "How strongly the model detects a sad or melancholy mood.", Signal(models, "mood sad", "sad"));
        AddSignal("Relaxed", "How strongly the model detects a relaxed character.", Signal(models, "mood relaxed", "relaxed"));
        AddSignal("Aggressive", "How strongly the model detects an aggressive character.", Signal(models, "mood aggressive", "aggressive"));
        AddSignal("Party", "How strongly the model detects a party-oriented character.", Signal(models, "mood party", "party"));
        AddSignal("Danceable", "How strongly the model classifies the track as danceable.", Signal(models, "danceability classifier", "danceable"));
        AddSignal("Vocal", "How strongly the model detects vocals rather than an instrumental track.", Signal(models, "voice/instrumental classifiers", "voice"));
        void AddSignal(string name, string explanation, double? score)
        {
            if (score is null) return;
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("82,*,40"), RowDefinitions = new RowDefinitions("Auto,Auto") };
            var brush = AnalysisColorScale.Mood(score.Value);
            var title = new TextBlock { Text = name, FontSize = 11, Foreground = brush };
            ToolTip.SetTip(title, explanation);
            var bar = new ProgressBar { Minimum = 0, Maximum = 1, Value = score.Value, Height = 6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = brush };
            var value = new TextBlock { Text = score.Value.ToString("0.##"), FontSize = 10.5, Foreground = brush, FontWeight = Avalonia.Media.FontWeight.SemiBold, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            Grid.SetColumn(bar, 1); Grid.SetColumn(value, 2);
            row.Children.Add(title); row.Children.Add(bar); row.Children.Add(value);
            MoodSignalsPanel.Children.Add(row);
        }

        void AddDerivedAttribute(DerivedTrackAttribute attribute)
        {
            var options = AttributeOptions(attribute.Key);
            if (options.Length == 0) return;
            var manualValue = _pendingAttributeOverrides.TryGetValue(attribute.Key, out var pendingValue)
                ? pendingValue
                : attribute.ManualValue;

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("72,*"),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            var label = new TextBlock
            {
                Text = FormatAttributeName(attribute.Key),
                FontSize = 11,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            ToolTip.SetTip(label, AttributeExplanation(attribute.Key));

            var choices = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            var choiceBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#3B4550")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Child = choices
            };

            for (var index = 0; index < options.Length; index++)
            {
                var option = options[index];
                var isSystem = string.Equals(option, attribute.SystemValue, StringComparison.OrdinalIgnoreCase);
                var optionButton = CreateProfileChoice(
                    option,
                    isSystem,
                    isSelected: isSystem ? manualValue is null : string.Equals(manualValue, option, StringComparison.OrdinalIgnoreCase),
                    isMiddle: index > 0 && index < options.Length - 1,
                    isFirst: index == 0,
                    isLast: index == options.Length - 1);
                optionButton.Click += (_, _) => SetOverride(isSystem ? null : option);
                choices.Children.Add(optionButton);
            }

            void SetOverride(string? value)
            {
                if (_buildingSoundProfile) return;
                _pendingAttributeOverrides[attribute.Key] = value;
                ShowSoundProfile(track);
                UpdateSaveButton();
            }

            Grid.SetColumn(choiceBorder, 1);
            row.Children.Add(label);
            row.Children.Add(choiceBorder);
            var targetPanel = attribute.Key is "energy_context" or "vocal_presence"
                ? SoundProfileRightPanel
                : SoundProfileLeftPanel;
            targetPanel.Children.Add(row);
        }

        static Button CreateProfileChoice(string value, bool isSystem, bool isSelected, bool isMiddle, bool isFirst, bool isLast)
        {
            var button = new Button
            {
                Content = value,
                FontSize = 10.5,
                Width = 84,
                Padding = new Avalonia.Thickness(9, 4),
                Margin = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(
                    isFirst ? 4 : 0,
                    isLast ? 4 : 0,
                    isLast ? 4 : 0,
                    isFirst ? 4 : 0),
                Background = new SolidColorBrush(Color.Parse(isSelected
                    ? (isSystem ? "#164968" : "#35414D")
                    : "#1E242A")),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                BorderBrush = new SolidColorBrush(Color.Parse("#53606C")),
                BorderThickness = isMiddle ? new Avalonia.Thickness(1, 0) : new Avalonia.Thickness(0),
                Foreground = new SolidColorBrush(Color.Parse(isSystem ? "#9BD8F8" : "#D8E0E8"))
            };
            ToolTip.SetTip(button, isSystem
                ? "Model suggestion. Click to use this value again."
                : "Manual override. Click to save this value instead of the model suggestion.");
            return button;
        }

        void AddMirexCharacter(IReadOnlyList<ExperimentalAnalysisModel> analysisModels)
        {
            var values = analysisModels.FirstOrDefault(model => model.Model == "moods mirex")?.Values
                .OrderByDescending(value => value.Score).ToList() ?? [];
            if (values.Count == 0) return;

            var panel = new StackPanel { Spacing = 4 };
            foreach (var value in values)
            {
                var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,40") };
                var brush = AnalysisColorScale.MoodModel(value.Score);
                var label = new TextBlock { Text = value.Label, FontSize = 10.5, Foreground = brush, TextWrapping = TextWrapping.Wrap };
                ToolTip.SetTip(label, MirexExplanation(value.Label));
                row.Children.Add(label);
                var score = new TextBlock
                {
                    Text = value.Score.ToString("0.##"),
                    FontSize = 10.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = brush,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                };
                Grid.SetColumn(score, 1);
                row.Children.Add(score);
                panel.Children.Add(row);
            }
            MirexCharacterPanel.Children.Add(panel);
        }
    }

    private static string[] AttributeOptions(string key) => key switch
    {
        "intensity" => ["Low", "Medium", "High"],
        "emotional_tone" => ["Melancholic", "Neutral", "Positive"],
        "energy_context" => ["Calm", "Driving", "Intense"],
        "vocal_presence" => ["Instrumental", "Mixed", "Vocal"],
        _ => []
    };

    private static string AttributeExplanation(string key) => key switch
    {
        "intensity" => "System suggestion based on several signals such as arousal, engagement and danceability.",
        "emotional_tone" => "System suggestion based primarily on the valence model output.",
        "energy_context" => "System suggestion based on the arousal model output.",
        "vocal_presence" => "System suggestion based on the voice versus instrumental classifier.",
        _ => "System-generated analysis attribute."
    };

    private static string MirexExplanation(string label) => label switch
    {
        var text when text.StartsWith("literate", StringComparison.OrdinalIgnoreCase) =>
            "Poignant bedeutet emotional berührend; wistful bedeutet sehnsüchtig oder nostalgisch; bittersweet bedeutet zugleich schön und traurig; brooding bedeutet nach innen gekehrt und düster.",
        var text when text.StartsWith("passionate", StringComparison.OrdinalIgnoreCase) =>
            "A forceful, confident and energetic emotional character.",
        var text when text.StartsWith("rollicking", StringComparison.OrdinalIgnoreCase) =>
            "A cheerful, light-hearted and playful emotional character.",
        var text when text.StartsWith("humorous", StringComparison.OrdinalIgnoreCase) =>
            "A quirky, humorous or whimsical emotional character.",
        var text when text.StartsWith("aggressive", StringComparison.OrdinalIgnoreCase) =>
            "A tense, fiery, anxious or forceful emotional character.",
        _ => "A broad MIREX mood cluster derived from several descriptive terms."
    };

    private static double? Signal(IReadOnlyList<ExperimentalAnalysisModel> models, string model, string label) =>
        models.FirstOrDefault(item => item.Model == model)?.Values.FirstOrDefault(value => value.Label == label)?.Score;

    private static string FormatAttributeName(string key) => key switch
    {
        "emotional_tone" => "Tone",
        "energy_context" => "Energy",
        "vocal_presence" => "Vocals",
        _ => char.ToUpperInvariant(key[0]) + key[1..]
    };

    private static string GetTempoInsight(double bpm) => bpm switch
    {
        < 80 => "Slow tempo — unhurried pace.",
        < 120 => "Moderate tempo — steady mid-tempo pace.",
        < 140 => "Medium-fast tempo — forward-moving pace.",
        < 175 => "Fast tempo — energetic pace.",
        _ => "Very fast tempo — high-energy pace."
    };

    private static string GetIntegratedLoudnessInsight(double lufs) => lufs switch
    {
        >= -8 => "Very loud overall — tightly mastered.",
        >= -11 => "Loud overall — modern, present master.",
        >= -14 => "Moderate loudness — more breathing room.",
        _ => "Relatively quiet overall — softer master."
    };

    private static string GetLoudnessRangeInsight(double lu) => lu switch
    {
        <= 3 => "Very even level — little contrast between sections.",
        <= 6 => "Controlled dynamics — moderate variation.",
        <= 10 => "Noticeable dynamics — clear quiet/loud contrast.",
        _ => "High dynamics — strong contrast across the track."
    };

    private void ShowModelSelectedGenres(MusicTrack track)
    {
        LoadModelMetadata();
        var assignments = CurrentModelGenreAssignments(track);
        _modelGenreIds = assignments.Select(assignment => assignment.GenreId).ToHashSet();
        ModelSelectedGenresSection.IsVisible = _modelSubgenresById.Count > 0;
        ModelSelectedGenresPanel.Children.Clear();
        foreach (var assignment in assignments)
        {
            var isManualSelection = assignment.Reasons.Count == 0;
            var confidenceBrush = AnalysisColorScale.GenreConfidence(
                isManualSelection ? 0.42 : assignment.Reasons.Max(reason => reason.Score));
            var container = new Border
            {
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(9, 5),
                Margin = new Avalonia.Thickness(0, 0, 6, 6),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto"), ColumnSpacing = 7 };
            var genreName = new TextBlock
            {
                Text = assignment.GenreName,
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(1, 0, 0, 0)
            };
            var strongestReason = assignment.Reasons
                .OrderByDescending(reason => reason.Score)
                .FirstOrDefault();
            var reason = new TextBlock
            {
                Text = isManualSelection ? string.Empty : $"{strongestReason!.Score:P0}",
                IsVisible = !isManualSelection,
                FontSize = 10.5,
                Foreground = confidenceBrush,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = isManualSelection ? 0.72 : 0.92
            };
            Grid.SetColumn(reason, 1);
            row.Children.Add(genreName);
            row.Children.Add(reason);
            container.Child = row;
            var enabled = assignment.IsEnabled;
            ApplyModelGenreVisual(container, genreName, reason, enabled, confidenceBrush, isManualSelection);
            container.PointerPressed += (_, _) =>
            {
                if (_pendingEnabledModelGenreIds.Contains(assignment.GenreId))
                    _pendingEnabledModelGenreIds.Remove(assignment.GenreId);
                else
                    _pendingEnabledModelGenreIds.Add(assignment.GenreId);
                ShowModelSelectedGenres(track);
                ShowDetectedGenres(track);
                RebuildModelGenreChoices();
                UpdateSaveButton();
            };
            IEnumerable<int> tooltipIds = isManualSelection
                ? new[] { assignment.GenreId }
                : assignment.Reasons
                    .Select(reason => FindModelSubgenreId(reason.ModelGenreName, reason.ModelSubgenreName))
                    .Where(id => id is not null)
                    .Select(id => id!.Value);
            ToolTip.SetTip(container, CreateModelMetadataTooltip(tooltipIds));
            ModelSelectedGenresPanel.Children.Add(container);
        }
    }

    private List<TrackModelGenre> CurrentModelGenreAssignments(MusicTrack track)
    {
        var predictions = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id);
        var predictionReasonsBySubgenreId = predictions
            .GroupBy(prediction => prediction.ModelSubgenreId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ModelGenreReason>)group
                    .Select(prediction => new ModelGenreReason(
                        prediction.ModelGenreName,
                        prediction.ModelSubgenreName,
                        prediction.Score))
                    .ToList());

        var assignments = MusicLibraryService.Current.GetTrackModelGenres(track.Id)
            .ToDictionary(assignment => assignment.GenreId);

        foreach (var genreId in _pendingEnabledModelGenreIds)
        {
            if (assignments.ContainsKey(genreId) || !_modelSubgenresById.TryGetValue(genreId, out var subgenre))
                continue;

            assignments[genreId] = new TrackModelGenre(
                genreId,
                subgenre.Name,
                true,
                predictionReasonsBySubgenreId.GetValueOrDefault(genreId, []));
        }

        return assignments.Values
            .Where(assignment => _initialEnabledModelGenreIds.Contains(assignment.GenreId)
                                 || _pendingEnabledModelGenreIds.Contains(assignment.GenreId))
            .Select(assignment => assignment with
            {
                IsEnabled = _pendingEnabledModelGenreIds.Contains(assignment.GenreId)
            })
            .OrderByDescending(assignment => assignment.IsEnabled)
            .ThenByDescending(assignment => assignment.Reasons.Count == 0 ? 0 : assignment.Reasons.Max(reason => reason.Score))
            .ThenBy(assignment => assignment.GenreName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ShowDetectedGenres(MusicTrack track)
    {
        LoadModelMetadata();
        var detected = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
            .Where(prediction => prediction.Score is > .05 and <= .25)
            .Where(prediction => !_modelGenreIds.Contains(prediction.ModelSubgenreId))
            .Take(6)
            .ToList();
        _visibleDetectedModelGenreIds = detected.Select(prediction => prediction.ModelSubgenreId).ToHashSet();
        DetectedGenresSection.IsVisible = detected.Count > 0;
        DetectedGenresPanel.Children.Clear();
        foreach (var prediction in detected)
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161C22")),
                BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(8, 5),
                Margin = new Avalonia.Thickness(0, 0, 6, 6)
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
            var confidenceBrush = AnalysisColorScale.GenreConfidence(prediction.Score);
            row.Children.Add(new TextBlock
            {
                Text = prediction.ModelSubgenreName,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = confidenceBrush,
                Margin = new Avalonia.Thickness(0, 0, 8, 0)
            });
            var modelGenre = new TextBlock
            {
                Text = prediction.ModelGenreName,
                FontSize = 10.5,
                Opacity = 0.62,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(modelGenre, 1);
            row.Children.Add(modelGenre);
            var score = new TextBlock { Text = prediction.Score.ToString("0.###"), FontSize = 10.5, Foreground = confidenceBrush, FontWeight = FontWeight.SemiBold };
            Grid.SetColumn(score, 2);
            row.Children.Add(score);
            container.Child = row;
            container.PointerPressed += (_, _) =>
            {
                _pendingEnabledModelGenreIds.Add(prediction.ModelSubgenreId);
                ShowModelSelectedGenres(track);
                ShowDetectedGenres(track);
                RebuildModelGenreChoices();
                UpdateSaveButton();
            };
            ToolTip.SetTip(container, CreateModelMetadataTooltip([prediction.ModelSubgenreId]));
            DetectedGenresPanel.Children.Add(container);
        }
        RebuildModelGenreChoices();
    }

    private void RebuildModelGenreChoices()
    {
        if (_modelSubgenresById.Count == 0)
        {
            AddModelGenreSection.IsVisible = false;
            return;
        }

        AddModelGenreSection.IsVisible = true;
        _buildingModelGenreChoices = true;
        var categoryChoices = new[] { new ModelGenreFilterChoice(null, "All model groups") }
            .Concat(_modelGenreNamesById
                .OrderBy(item => item.Value, StringComparer.OrdinalIgnoreCase)
                .Select(item => new ModelGenreFilterChoice(item.Key, item.Value)))
            .ToList();
        ModelGenreCategoryFilterBox.ItemsSource = categoryChoices;
        var selectedIndex = categoryChoices.FindIndex(choice => choice.Id == _modelGenreFilterId);
        ModelGenreCategoryFilterBox.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        _buildingModelGenreChoices = false;

        var search = _modelGenreSearchText.Trim();
        var choices = _modelSubgenresById.Values
            .Where(subgenre => !_modelGenreIds.Contains(subgenre.Id))
            .Where(subgenre => !_visibleDetectedModelGenreIds.Contains(subgenre.Id))
            .Where(subgenre => _modelGenreFilterId is null || subgenre.ModelGenreId == _modelGenreFilterId)
            .Where(subgenre => string.IsNullOrWhiteSpace(search)
                || subgenre.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(subgenre => _modelGenreNamesById.GetValueOrDefault(subgenre.ModelGenreId, ""), StringComparer.OrdinalIgnoreCase)
            .ThenBy(subgenre => subgenre.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ModelGenreChoicesPanel.Children.Clear();
        BuildFrequentManualGenreChoices();
        foreach (var subgenre in choices)
            ModelGenreChoicesPanel.Children.Add(CreateModelGenreChoiceButton(subgenre));

        ModelGenreChoiceSummaryText.Text = choices.Count == 0
            ? "No matching genres left."
            : $"{choices.Count} selectable"
              + (_modelGenreIds.Count > 0 ? $" · {_modelGenreIds.Count} active hidden" : string.Empty)
              + (_visibleDetectedModelGenreIds.Count > 0 ? $" · {_visibleDetectedModelGenreIds.Count} model detections shown above" : string.Empty);
    }

    private void BuildFrequentManualGenreChoices()
    {
        FrequentManualGenresPanel.Children.Clear();

        var search = _modelGenreSearchText.Trim();
        var frequentGenres = MusicLibraryService.Current.GetTopManualModelGenres()
            .Where(usage => !_modelGenreIds.Contains(usage.ModelSubgenreId))
            .Where(usage => !_visibleDetectedModelGenreIds.Contains(usage.ModelSubgenreId))
            .Where(usage => _modelSubgenresById.ContainsKey(usage.ModelSubgenreId))
            .Where(usage => string.IsNullOrWhiteSpace(search)
                || usage.ModelSubgenreName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();

        FrequentManualGenresSection.IsVisible = frequentGenres.Count > 0;
        foreach (var usage in frequentGenres)
            FrequentManualGenresPanel.Children.Add(CreateFrequentManualGenreButton(usage));
    }

    private Button CreateFrequentManualGenreButton(ManualModelGenreUsage usage)
    {
        var text = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = usage.ModelSubgenreName,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = usage.ModelGenreName,
            FontSize = 9,
            Opacity = 0.64,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 6 };
        content.Children.Add(text);
        var count = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#17384B")),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = $"{usage.UsageCount}×",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.Parse("#8FD8F7"))
            }
        };
        Grid.SetColumn(count, 1);
        content.Children.Add(count);

        var button = new Button
        {
            Content = content,
            Height = 44,
            Margin = new Avalonia.Thickness(0, 0, 6, 6),
            Padding = new Avalonia.Thickness(9, 4),
            Background = new SolidColorBrush(Color.Parse("#1B222A")),
            BorderBrush = new SolidColorBrush(Color.Parse("#455664")),
            CornerRadius = new Avalonia.CornerRadius(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(button, CreateModelMetadataTooltip([usage.ModelSubgenreId]));
        button.Click += (_, _) =>
        {
            if (_track is null) return;
            _pendingEnabledModelGenreIds.Add(usage.ModelSubgenreId);
            ShowModelSelectedGenres(_track);
            ShowDetectedGenres(_track);
            RebuildModelGenreChoices();
            UpdateSaveButton();
        };
        return button;
    }

    private Button CreateModelGenreChoiceButton(ModelSubgenre subgenre)
    {
        var genreName = _modelGenreNamesById.GetValueOrDefault(subgenre.ModelGenreId, "Genre");
        var text = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock
        {
            Text = subgenre.Name,
            FontSize = 10.5,
            FontWeight = FontWeight.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        text.Children.Add(new TextBlock
        {
            Text = genreName,
            FontSize = 9,
            Opacity = 0.58,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        var content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        content.Children.Add(text);
        var add = new TextBlock
        {
            Text = "+",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#74C8EE")),
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.8
        };
        Grid.SetColumn(add, 1);
        content.Children.Add(add);

        var button = new Button
        {
            Content = content,
            Height = 42,
            Margin = new Avalonia.Thickness(0, 0, 6, 6),
            Padding = new Avalonia.Thickness(9, 3),
            Background = new SolidColorBrush(Color.Parse("#161C22")),
            BorderBrush = new SolidColorBrush(Color.Parse("#344451")),
            CornerRadius = new Avalonia.CornerRadius(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        ToolTip.SetTip(button, CreateModelMetadataTooltip([subgenre.Id]));
        button.Click += (_, _) =>
        {
            if (_track is null) return;
            _pendingEnabledModelGenreIds.Add(subgenre.Id);
            ShowModelSelectedGenres(_track);
            ShowDetectedGenres(_track);
            RebuildModelGenreChoices();
            UpdateSaveButton();
        };
        return button;
    }

    private void OnModelGenreFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_buildingModelGenreChoices)
            return;
        _modelGenreFilterId = ModelGenreCategoryFilterBox.SelectedItem is ModelGenreFilterChoice choice
            ? choice.Id
            : null;
        RebuildModelGenreChoices();
    }

    private void OnModelGenreSearchChanged(object? sender, TextChangedEventArgs e)
    {
        _modelGenreSearchText = ModelGenreSearchBox.Text ?? string.Empty;
        RebuildModelGenreChoices();
    }

    private void LoadModelMetadata()
    {
        var subgenres = MusicLibraryService.Current.GetModelSubgenres();
        _modelSubgenresById = subgenres.ToDictionary(item => item.Id);
        _modelGenreNamesById = MusicLibraryService.Current.GetModelGenres().ToDictionary(item => item.Id, item => item.Name);
        _distinctionsBySubgenreId = MusicLibraryService.Current.GetModelSubgenreDistinctions()
            .GroupBy(item => item.ModelSubgenreId)
            .ToDictionary(group => group.Key, group => group.ToList());
    }

    private int? FindModelSubgenreId(string genreName, string subgenreName) => _modelSubgenresById.Values
        .FirstOrDefault(item => item.Name == subgenreName
            && _modelGenreNamesById.GetValueOrDefault(item.ModelGenreId) == genreName)?.Id;

    private Control CreateModelMetadataTooltip(IEnumerable<int> subgenreIds)
    {
        var panel = new StackPanel { Spacing = 7 };
        foreach (var id in subgenreIds.Distinct())
        {
            if (!_modelSubgenresById.TryGetValue(id, out var subgenre)) continue;
            var genreName = _modelGenreNamesById.GetValueOrDefault(subgenre.ModelGenreId, "Model genre");
            panel.Children.Add(new TextBlock
            {
                Text = $"{genreName} → {subgenre.Name}",
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#9FD7F2"))
            });
            if (!string.IsNullOrWhiteSpace(subgenre.Description))
                panel.Children.Add(new TextBlock { Text = subgenre.Description, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            if (!string.IsNullOrWhiteSpace(subgenre.ClassificationHint))
                panel.Children.Add(new TextBlock
                {
                    Text = $"Classify when: {subgenre.ClassificationHint}", FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.Parse("#8CC9E9")), TextWrapping = TextWrapping.Wrap
                });
            if (subgenre.BpmMin is not null || subgenre.BpmMax is not null)
                panel.Children.Add(new TextBlock
                {
                    Text = subgenre.BpmMin is not null && subgenre.BpmMax is not null
                        ? $"Typical BPM: {subgenre.BpmMin}–{subgenre.BpmMax}"
                        : $"Typical BPM: {(subgenre.BpmMin is not null ? $"from {subgenre.BpmMin}" : $"up to {subgenre.BpmMax}")}",
                    FontSize = 10.5, Opacity = 0.78
                });
            if (_distinctionsBySubgenreId.TryGetValue(subgenre.Id, out var distinctions))
            {
                panel.Children.Add(new TextBlock { Text = "Distinguish from", FontSize = 10.5, FontWeight = FontWeight.SemiBold, Opacity = 0.82 });
                foreach (var distinction in distinctions)
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{distinction.ModelGenreName} → {distinction.ModelSubgenreName}: {distinction.Difference}",
                        FontSize = 10, Opacity = 0.76, TextWrapping = TextWrapping.Wrap
                    });
            }
        }
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1B222A")), BorderBrush = new SolidColorBrush(Color.Parse("#4A667A")),
            BorderThickness = new Avalonia.Thickness(1), CornerRadius = new Avalonia.CornerRadius(6),
            // Tooltips opened near the left analysis column only have roughly 320 px of safe popup space.
            // Keep the content narrower than that space so text wraps instead of being clipped on both sides.
            Padding = new Avalonia.Thickness(12, 10), Width = 300, MaxWidth = 300,
            Child = new ScrollViewer { MaxHeight = 390, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = panel }
        };
    }

    private static void ApplyModelGenreVisual(
        Border container,
        TextBlock genreName,
        TextBlock detail,
        bool enabled,
        IBrush confidenceBrush,
        bool isManualSelection)
    {
        var manualEnabledBrush = new SolidColorBrush(Color.Parse("#BDEFFF"));
        var manualDisabledBrush = new SolidColorBrush(Color.Parse("#89949F"));
        var disabledBrush = new SolidColorBrush(Color.Parse("#A3ABB5"));
        container.BorderThickness = new Avalonia.Thickness(1);
        container.Background = new SolidColorBrush(Color.Parse(enabled
            ? isManualSelection ? "#123D5C" : "#153B54"
            : "#23272D"));
        container.BorderBrush = enabled
            ? isManualSelection ? new SolidColorBrush(Color.Parse("#36C4F1")) : confidenceBrush
            : new SolidColorBrush(Color.Parse("#3B414A"));
        genreName.Foreground = enabled
            ? isManualSelection ? manualEnabledBrush : confidenceBrush
            : isManualSelection ? manualDisabledBrush : disabledBrush;
        detail.Foreground = enabled
            ? isManualSelection ? new SolidColorBrush(Color.Parse("#7FE3FF")) : confidenceBrush
            : isManualSelection ? manualDisabledBrush : disabledBrush;
        detail.Opacity = enabled || !isManualSelection ? 0.92 : 0.54;
    }

    private sealed record ModelGenreFilterChoice(int? Id, string Label)
    {
        public override string ToString() => Label;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => CloseOverlay();

    private void OnKeepEditingClicked(object? sender, RoutedEventArgs e) =>
        UnsavedChangesLayer.IsVisible = false;

    private void OnDiscardChangesClicked(object? sender, RoutedEventArgs e) =>
        CloseOverlay(skipUnsavedCheck: true);

    private void OnSaveAndCloseClicked(object? sender, RoutedEventArgs e)
    {
        var trackId = _track?.Id;
        if (SaveCurrentChanges(closeAfterSave: true))
            TrackSaved?.Invoke(trackId!.Value);
    }

    private void CloseOverlay(bool skipUnsavedCheck = false)
    {
        if (!skipUnsavedCheck && HasUnsavedChanges())
        {
            UnsavedChangesLayer.IsVisible = true;
            return;
        }

        if (_isPlayingPreview)
        {
            _isPlayingPreview = false;
            PreviewClosed?.Invoke();
        }
        _analysisElapsedTimer.Stop();
        UnsavedChangesLayer.IsVisible = false;
        UnsavedChangesBadge.IsVisible = false;
        IsVisible = false;
        _track = null;
    }

    private void UpdateAnalysisElapsedTime()
    {
        var elapsed = DateTime.UtcNow - _analysisStartedAt;
        AnalysisElapsedText.Text = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes}:{elapsed.Seconds:00}";
    }
}
