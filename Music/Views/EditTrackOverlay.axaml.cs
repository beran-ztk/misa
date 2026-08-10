using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Threading;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class EditTrackOverlay : UserControl
{
    private static readonly TimeSpan OpenAnimationDuration = TimeSpan.FromMilliseconds(930);
    private static readonly IEasing SlideEasing = new SplineEasing(0.25, 0.1, 0.25, 1);

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
    private bool _areDetectedGenresExpanded;
    private int? _modelGenreFilterId;
    private string _modelGenreSearchText = string.Empty;
    private bool _buildingModelGenreChoices;
    private readonly DispatcherTimer _analysisElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _analysisStartedAt;
    private bool _isPlayingPreview;
    private bool _loadingTrack;
    private bool _isPublic;
    private bool _initialIsPublic;
    private string _initialTitle = string.Empty;
    private bool _isEditingInformation;
    private int _motionGeneration;
    private bool _isClosing;
    private int? _initialRatingId;
    private HashSet<int> _initialTagIds = [];
    private HashSet<int> _initialStyleIds = [];
    private HashSet<int> _initialEnabledModelGenreIds = [];
    private HashSet<int> _pendingEnabledModelGenreIds = [];

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
        var motionGeneration = ++_motionGeneration;
        _isClosing = false;
        _track = track;
        _isPlayingPreview = false;
        _areDetectedGenresExpanded = false;
        LoadLookups();
        Prefill(track);
        IsVisible = true;
        StartSlideAnimation(opening: true);
        if (analyzeAfterOpening)
            _ = AnalyzeAfterOpeningAsync(track, motionGeneration);
    }

    private bool StartSlideAnimation(bool opening)
    {
        var visual = ElementComposition.GetElementVisual(EditorSurface);
        if (visual is null)
            return false;

        var travel = EditorTravelDistance();
        var start = opening ? new Vector3(0, (float)travel, 0) : Vector3.Zero;
        var end = opening ? Vector3.Zero : new Vector3(0, (float)travel, 0);

        visual.Offset = new Avalonia.Vector3D(start.X, start.Y, start.Z);

        var animation = visual.Compositor.CreateVector3KeyFrameAnimation();
        animation.Duration = OpenAnimationDuration;
        animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        animation.InsertKeyFrame(0f, start);
        animation.InsertKeyFrame(1f, end, SlideEasing);
        visual.StartAnimation("Offset", animation);
        return true;
    }

    private double EditorTravelDistance()
    {
        var parentHeight = (Parent as Control)?.Bounds.Height ?? 0;
        var travel = Math.Max(Bounds.Height, parentHeight - Margin.Top - Margin.Bottom);
        return travel > 1 ? travel : 320;
    }

    private async Task AnalyzeAfterOpeningAsync(MusicTrack track, int motionGeneration)
    {
        await Task.Delay(OpenAnimationDuration);
        if (motionGeneration == _motionGeneration && IsVisible && !_isClosing)
            await AnalyzeImportedTrackAsync(track);
    }

    public void RequestClose() => CloseOverlay();

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
        ShowExperimentalAnalysis(track);
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
        EditInformationButton.Background = isEditing
            ? ThemeResources.Brush("Theme.Brush.AccentSurface")
            : Brushes.Transparent;
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
        button.Classes.Add("edit-chip");
        ApplyTagVisual(button, tag);
        return button;
    }

    private static void ApplyTagVisual(ToggleButton button, Tag tag)
    {
        var selected = button.IsChecked == true;
        button.Background = ThemeResources.Brush(selected
            ? "Theme.Brush.AccentSurface"
            : "Theme.Brush.Surface");
        button.BorderBrush = ThemeResources.Brush(selected
            ? "Theme.Brush.Accent"
            : "Theme.Brush.BorderSubtle");
        button.BorderThickness = new Avalonia.Thickness(1);
        if (button.Tag is TextBlock label)
        {
            label.Foreground = ThemeResources.Brush(selected
                ? "Theme.Brush.TextStrong"
                : "Theme.Brush.TextPrimary");
        }
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
        RatingBox.Background = ThemeResources.Brush("Theme.Brush.Input");
        RatingBox.BorderBrush = ThemeResources.Brush("Theme.Brush.BorderStrong");
        RatingBox.Foreground = RatingForeground(name);
    }

    private static IBrush RatingForeground(string name) => name switch
    {
        "Favorite" => ThemeResources.Brush("Theme.Brush.Warning"),
        "Great" => ThemeResources.Brush("Theme.Brush.AccentStrong"),
        "Good" => ThemeResources.Brush("Theme.Brush.TextPrimary"),
        "Okay" => ThemeResources.Brush("Theme.Brush.TextSecondary"),
        RatingNames.Avoid => ThemeResources.Brush("Theme.Brush.DangerText"),
        _ => ThemeResources.Brush("Theme.Brush.TextMuted")
    };

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
        PublicVisibilityText.Foreground = ThemeResources.Brush(isPublic
            ? "Theme.Brush.TextStrong"
            : "Theme.Brush.TextMuted");
        PrivateVisibilityText.Foreground = ThemeResources.Brush(isPublic
            ? "Theme.Brush.TextMuted"
            : "Theme.Brush.TextStrong");
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

        _track = _track with
        {
            Title = TitleBox.Text!.Trim(),
            RatingId = SelectedRatingId(),
            IsPublic = _isPublic
        };
        CaptureChangeSnapshot(_track, SelectedTagIds(), styleIds.ToHashSet(), _pendingEnabledModelGenreIds);
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
        ShowExperimentalAnalysis(track);
        UpdateSaveButton();
    }

    private static string FormatEstimate(TimeSpan duration) => duration.TotalMinutes >= 1
        ? $"{Math.Ceiling(duration.TotalMinutes):0} min"
        : $"{Math.Max(1, Math.Round(duration.TotalSeconds)):0} sec";

    private void ShowAudioAnalysis(MusicTrack track)
    {
        var analysis = MusicLibraryService.Current.GetTrackAudioAnalysis(track.Id);
        AudioAnalysisSection.IsVisible = analysis is not null;
        AnalysisCharacterSection.IsVisible = AudioAnalysisSection.IsVisible;
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

    private void ShowExperimentalAnalysis(MusicTrack track)
    {
        var models = MusicLibraryService.Current.GetExperimentalAnalysis(track.Id);
        AnalysisCharacterSection.IsVisible = AudioAnalysisSection.IsVisible || models.Count > 0;
        MirexCharacterPanel.Children.Clear();
        if (models.Count == 0) return;

        AddMirexCharacter(models);

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
            var confidence = isManualSelection ? 0 : assignment.Reasons.Max(reason => reason.Score);
            var confidenceBrush = AnalysisColorScale.GenreConfidence(confidence);
            var enabled = assignment.IsEnabled;
            var container = new Border
            {
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0, 3),
                Opacity = enabled ? 1 : 0.48,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            var content = new Grid
            {
                RowDefinitions = new RowDefinitions(isManualSelection ? "Auto" : "Auto,Auto"),
                RowSpacing = 6
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
            var genreName = new TextBlock
            {
                Text = assignment.GenreName,
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                Foreground = ThemeResources.Brush("Theme.Brush.TextStrong"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            row.Children.Add(genreName);

            Control detail;
            if (isManualSelection)
            {
                detail = new TextBlock
                {
                    Text = "Edit manually",
                    FontSize = 10.5,
                    Foreground = ThemeResources.Brush("Theme.Brush.TextStrong"),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Opacity = 0.58
                };
            }
            else
            {
                detail = new TextBlock
                {
                    Text = $"{confidence:P0}",
                    FontSize = 10.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = confidenceBrush,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Opacity = 0.94
                };
                var confidenceBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 1,
                    Value = confidence,
                    Height = 5,
                    Foreground = confidenceBrush,
                    Background = ThemeResources.Brush("Theme.Brush.Surface")
                };
                Grid.SetRow(confidenceBar, 1);
                content.Children.Add(confidenceBar);
            }

            Grid.SetColumn(detail, 1);
            row.Children.Add(detail);
            content.Children.Add(row);
            container.Child = content;
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
            .Where(assignment => _pendingEnabledModelGenreIds.Contains(assignment.GenreId))
            .Select(assignment => assignment with
            {
                IsEnabled = true
            })
            .OrderByDescending(assignment => assignment.Reasons.Count == 0 ? 0 : assignment.Reasons.Max(reason => reason.Score))
            .ThenBy(assignment => assignment.GenreName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ShowDetectedGenres(MusicTrack track)
    {
        LoadModelMetadata();
        var detected = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
            .Where(prediction => !_modelGenreIds.Contains(prediction.ModelSubgenreId))
            .GroupBy(prediction => prediction.ModelSubgenreId)
            .Select(group => group.OrderByDescending(prediction => prediction.Score).First())
            .OrderByDescending(prediction => prediction.Score)
            .ThenBy(prediction => prediction.ModelSubgenreName, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _visibleDetectedModelGenreIds = detected.Select(prediction => prediction.ModelSubgenreId).ToHashSet();
        DetectedGenresSection.IsVisible = detected.Count > 0;
        DetectedGenresCountText.Text = detected.Count.ToString();
        DetectedGenresChevron.Text = _areDetectedGenresExpanded ? "⌄" : "›";
        DetectedGenresPanel.IsVisible = _areDetectedGenresExpanded;
        DetectedGenresPanel.Children.Clear();
        if (!_areDetectedGenresExpanded)
        {
            RebuildModelGenreChoices();
            return;
        }

        foreach (var prediction in detected)
        {
            var container = new Border
            {
                Background = Brushes.Transparent,
                BorderThickness = new Avalonia.Thickness(0),
                Padding = new Avalonia.Thickness(0, 4),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            var content = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = 5 };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
            var confidenceBrush = AnalysisColorScale.GenreConfidence(prediction.Score);
            row.Children.Add(new TextBlock
            {
                Text = prediction.ModelSubgenreName,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = ThemeResources.Brush("Theme.Brush.TextStrong"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var score = new TextBlock
            {
                Text = $"{prediction.Score:P0}",
                FontSize = 10.5,
                Foreground = confidenceBrush,
                FontWeight = FontWeight.SemiBold
            };
            Grid.SetColumn(score, 1);
            row.Children.Add(score);
            content.Children.Add(row);

            var detail = new Grid { ColumnDefinitions = new ColumnDefinitions("110,*"), ColumnSpacing = 8 };
            detail.Children.Add(new TextBlock
            {
                Text = prediction.ModelGenreName,
                FontSize = 9.5,
                Opacity = 0.52,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            var confidenceBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 1,
                Value = prediction.Score,
                Height = 4,
                Foreground = confidenceBrush,
                Background = ThemeResources.Brush("Theme.Brush.Surface"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(confidenceBar, 1);
            detail.Children.Add(confidenceBar);
            Grid.SetRow(detail, 1);
            content.Children.Add(detail);
            container.Child = content;
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

    private void OnDetectedGenresHeaderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_track is null)
            return;

        _areDetectedGenresExpanded = !_areDetectedGenresExpanded;
        ShowDetectedGenres(_track);
        e.Handled = true;
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
              + (_visibleDetectedModelGenreIds.Count > 0 ? $" · {_visibleDetectedModelGenreIds.Count} available under detections" : string.Empty);
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
            Background = ThemeResources.Brush("Theme.Brush.AccentSurface"),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(6, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = $"{usage.UsageCount}×",
                FontSize = 9,
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary")
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
            Background = ThemeResources.Brush("Theme.Brush.SurfaceRaised"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.Border"),
            CornerRadius = new Avalonia.CornerRadius(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Classes.Add("edit-choice");
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
            Foreground = ThemeResources.Brush("Theme.Brush.Accent"),
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
            Background = ThemeResources.Brush("Theme.Brush.Surface"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderSubtle"),
            CornerRadius = new Avalonia.CornerRadius(5),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Classes.Add("edit-choice");
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
                Foreground = ThemeResources.Brush("Theme.Brush.TextSecondary")
            });
            if (!string.IsNullOrWhiteSpace(subgenre.Description))
                panel.Children.Add(new TextBlock { Text = subgenre.Description, FontSize = 11, TextWrapping = TextWrapping.Wrap });
            if (!string.IsNullOrWhiteSpace(subgenre.ClassificationHint))
                panel.Children.Add(new TextBlock
                {
                    Text = $"Classify when: {subgenre.ClassificationHint}", FontSize = 10.5,
                    Foreground = ThemeResources.Brush("Theme.Brush.Accent"), TextWrapping = TextWrapping.Wrap
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
            Background = ThemeResources.Brush("Theme.Brush.SurfaceRaised"),
            BorderBrush = ThemeResources.Brush("Theme.Brush.BorderStrong"),
            BorderThickness = new Avalonia.Thickness(1), CornerRadius = new Avalonia.CornerRadius(6),
            // Tooltips opened near the left analysis column only have roughly 320 px of safe popup space.
            // Keep the content narrower than that space so text wraps instead of being clipped on both sides.
            Padding = new Avalonia.Thickness(12, 10), Width = 300, MaxWidth = 300,
            Child = new ScrollViewer { MaxHeight = 390, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = panel }
        };
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
        if (_isClosing)
            return;

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
        _isClosing = true;
        var motionGeneration = ++_motionGeneration;
        if (StartSlideAnimation(opening: false))
            _ = CompleteCloseAfterAnimationAsync(motionGeneration);
        else
            CompleteClose(motionGeneration);
    }

    private async Task CompleteCloseAfterAnimationAsync(int motionGeneration)
    {
        await Task.Delay(OpenAnimationDuration);
        CompleteClose(motionGeneration);
    }

    private void CompleteClose(int motionGeneration)
    {
        if (motionGeneration != _motionGeneration || !_isClosing)
            return;

        IsVisible = false;
        _track = null;
        _isClosing = false;
    }

    private void UpdateAnalysisElapsedTime()
    {
        var elapsed = DateTime.UtcNow - _analysisStartedAt;
        AnalysisElapsedText.Text = elapsed.TotalHours >= 1
            ? $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes}:{elapsed.Seconds:00}";
    }
}
