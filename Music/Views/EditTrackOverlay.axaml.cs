using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    private List<Genre> _genres = [];
    private List<Tag> _tags = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    private readonly List<(Genre Genre, ToggleButton Btn)> _genreChips = [];
    private readonly List<(Tag Tag, ToggleButton Btn)> _tagChips = [];
    private readonly List<(Style Style, ToggleButton Btn)> _styleChips = [];

    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private HashSet<int> _modelGenreIds = [];
    private Dictionary<int, ModelSubgenre> _modelSubgenresById = [];
    private Dictionary<int, string> _modelGenreNamesById = [];
    private Dictionary<int, List<ModelSubgenreDistinction>> _distinctionsBySubgenreId = [];
    private CancellationTokenSource? _analysisPreviewCancellation;
    private readonly Dictionary<string, string?> _pendingAttributeOverrides = [];
    private bool _buildingSoundProfile;
    private readonly DispatcherTimer _analysisElapsedTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private DateTime _analysisStartedAt;
    private bool _isPlayingPreview;

    public event Action? TrackSaved;
    public event Action<MusicTrack>? PreviewRequested;
    public event Action? PreviewClosed;

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
        StartModelAnalysisPreview();
        if (analyzeAfterOpening)
            _ = AnalyzeImportedTrackAsync(track);
    }

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _tags = MusicLibraryService.Current.GetTags();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        StylesSection.IsVisible = _styles.Count > 0;

        RatingBox.ItemsSource = _ratings.Select(r => r.Name).ToList();
    }

    private void Prefill(MusicTrack track)
    {
        _pendingAttributeOverrides.Clear();
        TitleBox.Text = track.Title;
        ChannelBox.Text = track.ChannelName ?? string.Empty;
        YouTubeUrlBox.Text = track.CanonicalUrl;
        ChannelUrlBox.Text = track.ChannelUrl ?? string.Empty;
        ChannelInfoRow.IsVisible = !string.IsNullOrWhiteSpace(track.ChannelName);
        ChannelUrlRow.IsVisible = !string.IsNullOrWhiteSpace(track.ChannelUrl);

        var ratingIndex = _ratings.FindIndex(r => r.Id == track.RatingId);
        RatingBox.SelectedIndex = ratingIndex;
        UpdateRatingVisual();

        var selectedGenreIds = MusicLibraryService.Current.GetTrackManualGenreIds(track.Id).ToHashSet();
        var selectedTagIds = MusicLibraryService.Current.GetTrackTagIds(track.Id).ToHashSet();
        var selectedStyleIds = MusicLibraryService.Current.GetTrackStyleIds(track.Id).ToHashSet();

        ShowModelSelectedGenres(track);
        ShowDetectedGenres(track);
        RebuildGenreChips(selectedGenreIds);
        RebuildTagChips(selectedTagIds);
        RebuildStyleChips(selectedStyleIds);
        ShowModelPredictions(track, applyMappedGenres: false);
        ShowAudioAnalysis(track);
        ShowSoundProfile(track);
        ShowUsageStats(track);
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
        TrackUsageFooter.IsVisible = usage.PlayCount > 0 || usage.ListenedSeconds > 0 || usage.SkipCount > 0;
        if (!TrackUsageFooter.IsVisible) return;
        var listened = usage.ListenedSeconds >= 60
            ? $"{usage.ListenedSeconds / 60} min listened"
            : $"{usage.ListenedSeconds} sec listened";
        TrackUsageText.Text = $"Listening · {usage.PlayCount} plays · {listened} · {usage.SkipCount} skips";
    }

    private void RebuildGenreChips(IReadOnlySet<int> selectedGenreIds)
    {
        var sorted = _genres
            .Where(genre => !_modelGenreIds.Contains(genre.Id))
            .OrderByDescending(g => selectedGenreIds.Contains(g.Id))
            .ThenBy(g => g.Name)
            .ToList();

        GenresPanel.Children.Clear();
        _genreChips.Clear();

        foreach (var genre in sorted)
        {
            var isSelected = selectedGenreIds.Contains(genre.Id);
            var btn = CreateManualGenreButton(genre.Name, isSelected);
            btn.IsCheckedChanged += (_, _) =>
            {
                ApplyManualGenreVisual(btn);
                UpdateManualGenreSummary();
                RebuildStyleChips();
                UpdateSaveButton();
            };

            _genreChips.Add((genre, btn));
            GenresPanel.Children.Add(btn);
        }

        UpdateManualGenreSummary();
    }

    private static ToggleButton CreateManualGenreButton(string name, bool isSelected)
    {
        var label = new TextBlock
        {
            Text = name,
            FontSize = 12,
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
            Height = 31,
            Margin = new Avalonia.Thickness(0, 0, 6, 6),
            Padding = new Avalonia.Thickness(9, 3),
            CornerRadius = new Avalonia.CornerRadius(3),
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Tag = label
        };
        ApplyManualGenreVisual(button);
        return button;
    }

    private static void ApplyManualGenreVisual(ToggleButton button)
    {
        var selected = button.IsChecked == true;
        button.Background = new SolidColorBrush(Color.Parse(selected ? "#164968" : "#1A2026"));
        button.BorderBrush = new SolidColorBrush(Color.Parse(selected ? "#45A7DC" : "#394653"));
        button.BorderThickness = new Avalonia.Thickness(1);
        if (button.Tag is TextBlock label)
            label.Foreground = new SolidColorBrush(Color.Parse(selected ? "#E9F6FF" : "#D7E0E8"));
    }

    private void UpdateManualGenreSummary()
    {
        var selectedCount = _genreChips.Count(item => item.Btn.IsChecked == true);
        ManualGenreSummaryText.Text = selectedCount == 0
            ? "No manual genres"
            : $"{selectedCount} selected";
    }

    private void RebuildTagChips(IReadOnlySet<int> selectedTagIds)
    {
        var sorted = _tags
            .OrderByDescending(tag => selectedTagIds.Contains(tag.Id))
            .ThenBy(tag => tag.CategoryName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(tag => tag.SortOrder)
            .ThenBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TagsPanel.Children.Clear();
        _tagChips.Clear();

        foreach (var tag in sorted)
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
            TagsPanel.Children.Add(btn);
        }

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
        ToolTip.SetTip(button, string.IsNullOrWhiteSpace(tag.Description)
            ? tag.CategoryName
            : $"{tag.CategoryName}\n{tag.Description}");
        ApplyTagVisual(button, tag);
        return button;
    }

    private static void ApplyTagVisual(ToggleButton button, Tag tag)
    {
        var selected = button.IsChecked == true;
        var accent = SafeBrush(tag.Color, "#65BCEB");
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

    private void RebuildStyleChips(IReadOnlySet<int>? selectedStyleIds = null)
    {
        var selectedGenreIds = SelectedGenreIds();
        var styleCounts = MetadataCountService.StyleCountsForGenres(
            _allTrackGenreIds, _allTrackStyleIds, selectedGenreIds);

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

    private HashSet<int> SelectedGenreIds() =>
        _genreChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Genre.Id)
            .ToHashSet();

    private HashSet<int> SelectedTagIds() =>
        _tagChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Tag.Id)
            .ToHashSet();

    private void UpdateSaveButton()
    {
        SaveBtn.IsEnabled = _track != null && !string.IsNullOrWhiteSpace(TitleBox.Text);
    }

    private void UpdateRatingVisual()
    {
        var name = RatingBox.SelectedIndex >= 0 && RatingBox.SelectedIndex < _ratings.Count
            ? _ratings[RatingBox.SelectedIndex].Name
            : "Not rated";
        var (background, border, foreground) = RatingColors(name);
        RatingBox.Background = background;
        RatingBox.BorderBrush = border;
        RatingBox.Foreground = foreground;
    }

    private static (IBrush Background, IBrush Border, IBrush Foreground) RatingColors(string name) => name switch
    {
        "Favorite" => (Brush("#3B341C"), Brush("#E7BE4B"), Brush("#FFE39A")),
        "Great" => (Brush("#183827"), Brush("#4EC27A"), Brush("#B7F2CC")),
        "Good" => (Brush("#17354B"), Brush("#4DA5DD"), Brush("#BCE7FF")),
        "Okay" => (Brush("#2C3440"), Brush("#8795A7"), Brush("#DAE1E9")),
        "Skip" => (Brush("#402326"), Brush("#DD6A70"), Brush("#FFC1C4")),
        _ => (Brush("#20252B"), Brush("#4C5967"), Brush("#BBC5CE"))
    };

    private static IBrush Brush(string color) => new SolidColorBrush(Color.Parse(color));

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        if (_track == null)
            return;

        var genreIds = SelectedGenreIds().ToList();
        var styleIds = _styleChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Style.Id)
            .ToList();

        MusicLibraryService.Current.UpdateTrack(
            _track.Id,
            TitleBox.Text!.Trim(),
            genreIds,
            RatingBox.SelectedIndex >= 0 ? _ratings[RatingBox.SelectedIndex].Id : null,
            styleIds);

        MusicLibraryService.Current.SetTrackManualTags(_track.Id, SelectedTagIds());

        foreach (var overrideValue in _pendingAttributeOverrides)
            MusicLibraryService.Current.SetTrackDerivedAttributeOverride(_track.Id, overrideValue.Key, overrideValue.Value);

        CloseOverlay();
        TrackSaved?.Invoke();
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
        ShowModelPredictions(track, applyMappedGenres: error is null);
        StartModelAnalysisPreview();
        ShowModelSelectedGenres(track);
        ShowDetectedGenres(track);
        RebuildGenreChips(SelectedGenreIds());
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

        BpmInsightText.Text = analysis.Bpm is double detectedBpm
            ? GetTempoInsight(detectedBpm)
            : "Tempo could not be determined.";
        IntegratedLoudnessInsightText.Text = analysis.IntegratedLoudness is double detectedLoudness
            ? GetIntegratedLoudnessInsight(detectedLoudness)
            : "Average loudness could not be determined.";
        LoudnessRangeInsightText.Text = analysis.LoudnessRange is double loudnessRange
            ? GetLoudnessRangeInsight(loudnessRange)
            : "Loudness variation could not be determined.";
    }

    private void ShowSoundProfile(MusicTrack track)
    {
        var models = MusicLibraryService.Current.GetExperimentalAnalysis(track.Id);
        var derived = MusicLibraryService.Current.GetTrackDerivedAttributes(track.Id);
        SoundProfileSection.IsVisible = models.Count > 0 || derived.Count > 0;
        SoundProfilePanel.Children.Clear();
        if (!SoundProfileSection.IsVisible) return;

        _buildingSoundProfile = true;
        foreach (var attribute in derived)
            AddDerivedAttribute(attribute);
        _buildingSoundProfile = false;

        // These compact model summaries belong next to the profile choices they explain,
        // rather than below a long list of raw confidence bars.
        AddJamendoThemes(models);
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
            SoundProfilePanel.Children.Add(row);
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
                ColumnDefinitions = new ColumnDefinitions("112,*"),
                Margin = new Avalonia.Thickness(0, 0, 0, 2)
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
            SoundProfilePanel.Children.Add(row);
        }

        static Button CreateProfileChoice(string value, bool isSystem, bool isSelected, bool isMiddle, bool isFirst, bool isLast)
        {
            var button = new Button
            {
                Content = value,
                FontSize = 10.5,
                Width = 96,
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

        void AddJamendoThemes(IReadOnlyList<ExperimentalAnalysisModel> analysisModels)
        {
            var tags = analysisModels.FirstOrDefault(model => model.Model == "mtg_jamendo_moodtheme")?.Values
                .OrderByDescending(value => value.Score).Take(5).ToList() ?? [];
            if (tags.Count == 0) return;
            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#151C25")),
                BorderBrush = new SolidColorBrush(Color.Parse("#29465A")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(10, 8),
                Margin = new Avalonia.Thickness(0, 1, 0, 2)
            };
            var panel = new StackPanel { Spacing = 5 };
            panel.Children.Add(new TextBlock
            {
                Text = "Themes · Jamendo mood/theme model",
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#78C7EE"))
            });
            var tagPanel = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
            foreach (var tag in tags)
            {
                var tagText = new TextBlock
                {
                    Text = $"{tag.Label}  {tag.Score:0.##}",
                    FontSize = 10.5,
                    Foreground = AnalysisColorScale.MoodModel(tag.Score),
                    Background = new SolidColorBrush(Color.Parse("#203747")),
                    Padding = new Avalonia.Thickness(6, 2),
                    Margin = new Avalonia.Thickness(0, 0, 5, 4)
                };
                ToolTip.SetTip(tagText, "A Jamendo tag: an independent theme or atmosphere detected by the model.");
                tagPanel.Children.Add(tagText);
            }
            panel.Children.Add(tagPanel);
            card.Child = panel;
            SoundProfilePanel.Children.Add(card);
        }

        void AddMirexCharacter(IReadOnlyList<ExperimentalAnalysisModel> analysisModels)
        {
            var values = analysisModels.FirstOrDefault(model => model.Model == "moods mirex")?.Values
                .OrderByDescending(value => value.Score).ToList() ?? [];
            if (values.Count == 0) return;

            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161C22")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2D3945")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(10, 8),
                Margin = new Avalonia.Thickness(0, 1, 0, 2)
            };
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock
            {
                Text = "Emotional character · MIREX mood clusters",
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#9BD8F8"))
            });
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
            card.Child = panel;
            SoundProfilePanel.Children.Add(card);
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

    private void ShowModelPredictions(MusicTrack track, bool applyMappedGenres)
    {
        const double displayThreshold = 0.1;
        var predictions = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
            .Where(prediction => prediction.Score > displayThreshold)
            .ToList();
        var experimentalModels = MusicLibraryService.Current.GetExperimentalAnalysis(track.Id);

        AnalysisButton.IsVisible = predictions.Count > 0 || experimentalModels.Count > 0;
        if (!AnalysisButton.IsVisible)
            AnalysisPopup.IsOpen = false;

        var sections = new List<string>();
        if (predictions.Count > 0)
        {
            sections.Add("Genres\n" + string.Join("\n", predictions.Select(prediction =>
                $"{prediction.ModelGenreName} → {prediction.ModelSubgenreName}  ({prediction.Score:0.###})")));
        }
        if (experimentalModels.Count > 0)
        {
            sections.Add("Experimental output — not saved\n" + string.Join("\n\n", experimentalModels
                .OrderBy(model => model.Family)
                .ThenBy(model => model.Category)
                .ThenBy(model => model.Model)
                .Select(FormatExperimentalModel)));
        }

        ModelPredictionsText.Text = string.Join("\n\n", sections);

    }

    private static string FormatExperimentalModel(ExperimentalAnalysisModel model)
    {
        IEnumerable<ExperimentalAnalysisValue> orderedValues = model.Values.OrderByDescending(value => value.Score);
        var isMoodThemeModel = model.Model.Equals("mtg_jamendo_moodtheme", StringComparison.OrdinalIgnoreCase);
        if (isMoodThemeModel)
            orderedValues = orderedValues.Take(5);
        var values = string.Join("\n", orderedValues
            .Select(value => $"  {value.Label}: {value.Score:0.###}"));
        var suffix = isMoodThemeModel ? " (top 5)" : string.Empty;
        return $"{model.Category} · {model.Model}{suffix}\n{values}";
    }

    private void OnAnalysisButtonClicked(object? sender, RoutedEventArgs e)
    {
        _analysisPreviewCancellation?.Cancel();
        AnalysisPopup.IsOpen = !AnalysisPopup.IsOpen;
    }

    private void OnEditorPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source != AnalysisButton)
            AnalysisPopup.IsOpen = false;
    }

    private void StartModelAnalysisPreview()
    {
        _analysisPreviewCancellation?.Cancel();
        if (!AnalysisButton.IsVisible)
            return;

        var cancellation = _analysisPreviewCancellation = new CancellationTokenSource();
        AnalysisPopup.IsOpen = true;
        _ = CloseAnalysisPreviewAfterDelayAsync(cancellation.Token);
    }

    private async System.Threading.Tasks.Task CloseAnalysisPreviewAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                AnalysisPopup.IsOpen = false;
        }
        catch (OperationCanceledException)
        {
            // A manual toggle or a closed editor cancels the temporary preview.
        }
    }

    private void ShowModelSelectedGenres(MusicTrack track)
    {
        LoadModelMetadata();
        var assignments = MusicLibraryService.Current.GetTrackModelGenres(track.Id);
        _modelGenreIds = assignments.Select(assignment => assignment.GenreId).ToHashSet();
        ModelSelectedGenresSection.IsVisible = assignments.Count > 0;
        ModelSelectedGenresPanel.Children.Clear();
        foreach (var assignment in assignments)
        {
            var confidenceBrush = AnalysisColorScale.GenreConfidence(
                assignment.Reasons.Count == 0 ? 0 : assignment.Reasons.Max(reason => reason.Score));
            var container = new Border
            {
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(6),
                Padding = new Avalonia.Thickness(9, 7),
                Margin = new Avalonia.Thickness(0, 0, 0, 3)
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*") };
            var genreName = new TextBlock
            {
                Text = assignment.GenreName,
                FontSize = 12,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(2, 0, 10, 0)
            };
            var divider = new Border
            {
                Width = 1,
                Height = 18,
                Background = new SolidColorBrush(Color.Parse("#4D7B96")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 10, 0)
            };
            var reason = new TextBlock
            {
                Text = string.Join(" · ", assignment.Reasons.Select(item => $"{item.ModelGenreName} → {item.ModelSubgenreName} ({item.Score:0.###})")),
                FontSize = 10.5, Foreground = confidenceBrush, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(divider, 1);
            Grid.SetColumn(reason, 2);
            row.Children.Add(genreName);
            row.Children.Add(divider);
            row.Children.Add(reason);
            container.Child = row;
            var enabled = assignment.IsEnabled;
            ApplyModelGenreVisual(container, genreName, divider, enabled, confidenceBrush);
            container.PointerPressed += (_, _) =>
            {
                enabled = !enabled;
                MusicLibraryService.Current.SetTrackModelGenreEnabled(track.Id, assignment.GenreId, enabled);
                ApplyModelGenreVisual(container, genreName, divider, enabled, confidenceBrush);
            };
            ToolTip.SetTip(container, CreateModelMetadataTooltip(assignment.Reasons
                .Select(reason => FindModelSubgenreId(reason.ModelGenreName, reason.ModelSubgenreName))
                .Where(id => id is not null)
                .Select(id => id!.Value)));
            ModelSelectedGenresPanel.Children.Add(container);
        }
    }

    private void ShowDetectedGenres(MusicTrack track)
    {
        LoadModelMetadata();
        var mappedSubgenreIds = MusicLibraryService.Current.GetGenreMappings()
            .Select(mapping => mapping.ModelSubgenreId).ToHashSet();
        var detected = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
            .Where(prediction => prediction.Score > .1 && !mappedSubgenreIds.Contains(prediction.ModelSubgenreId))
            .Take(6)
            .Select(prediction => $"{prediction.ModelGenreName} → {prediction.ModelSubgenreName}")
            .ToList();
        DetectedGenresSection.IsVisible = detected.Count > 0;
        DetectedGenresPanel.Children.Clear();
        foreach (var prediction in MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
                     .Where(prediction => prediction.Score > .1 && !mappedSubgenreIds.Contains(prediction.ModelSubgenreId))
                     .Take(6))
        {
            var container = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#161C22")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2D3945")),
                BorderThickness = new Avalonia.Thickness(1),
                CornerRadius = new Avalonia.CornerRadius(5),
                Padding = new Avalonia.Thickness(8, 5)
            };
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var confidenceBrush = AnalysisColorScale.GenreConfidence(prediction.Score);
            row.Children.Add(new TextBlock { Text = $"{prediction.ModelGenreName} → {prediction.ModelSubgenreName}", FontSize = 11, Foreground = confidenceBrush });
            var score = new TextBlock { Text = prediction.Score.ToString("0.###"), FontSize = 10.5, Foreground = confidenceBrush, FontWeight = FontWeight.SemiBold };
            Grid.SetColumn(score, 1);
            row.Children.Add(score);
            container.Child = row;
            ToolTip.SetTip(container, CreateModelMetadataTooltip([prediction.ModelSubgenreId]));
            DetectedGenresPanel.Children.Add(container);
        }
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

    private static void ApplyModelGenreVisual(Border container, TextBlock genreName, Border divider, bool enabled, IBrush confidenceBrush)
    {
        container.Background = new SolidColorBrush(Color.Parse(enabled ? "#153B54" : "#23272D"));
        container.BorderBrush = enabled ? confidenceBrush : new SolidColorBrush(Color.Parse("#3B414A"));
        genreName.Foreground = enabled ? confidenceBrush : new SolidColorBrush(Color.Parse("#A3ABB5"));
        divider.Background = enabled ? confidenceBrush : new SolidColorBrush(Color.Parse("#4A535D"));
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => CloseOverlay();

    private void CloseOverlay()
    {
        if (_isPlayingPreview)
        {
            _isPlayingPreview = false;
            PreviewClosed?.Invoke();
        }
        _analysisElapsedTimer.Stop();
        _analysisPreviewCancellation?.Cancel();
        AnalysisPopup.IsOpen = false;
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
