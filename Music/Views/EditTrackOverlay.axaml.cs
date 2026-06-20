using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class EditTrackOverlay : UserControl
{
    private MusicTrack? _track;
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    private readonly List<(Genre Genre, ToggleButton Btn)> _genreChips = [];
    private readonly List<(Style Style, ToggleButton Btn)> _styleChips = [];

    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];
    private HashSet<int> _modelGenreIds = [];
    private CancellationTokenSource? _analysisPreviewCancellation;

    public event Action? TrackSaved;

    public EditTrackOverlay()
    {
        InitializeComponent();
        TitleBox.TextChanged += (_, _) => UpdateSaveButton();
        RatingBox.SelectionChanged += (_, _) => UpdateSaveButton();
    }

    public void Open(MusicTrack track, bool analyzeAfterOpening = false)
    {
        _track = track;
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
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();
        StylesSection.IsVisible = _styles.Count > 0;

        RatingBox.ItemsSource = _ratings.Select(r => r.Name).ToList();
    }

    private void Prefill(MusicTrack track)
    {
        StatusText.Text = "";
        TitleBox.Text = track.Title;

        var ratingIndex = _ratings.FindIndex(r => r.Id == track.RatingId);
        RatingBox.SelectedIndex = ratingIndex >= 0 ? ratingIndex : -1;

        var selectedGenreIds = MusicLibraryService.Current.GetTrackManualGenreIds(track.Id).ToHashSet();
        var selectedStyleIds = MusicLibraryService.Current.GetTrackStyleIds(track.Id).ToHashSet();

        ShowModelSelectedGenres(track);
        RebuildGenreChips(selectedGenreIds);
        RebuildStyleChips(selectedStyleIds);
        ShowModelPredictions(track, applyMappedGenres: false);
        ShowAudioAnalysis(track);
        ShowSoundProfile(track);
        UpdateSaveButton();
    }

    private void RebuildGenreChips(IReadOnlySet<int> selectedGenreIds)
    {
        var genreCounts = MetadataCountService.GenreCounts(_allTrackGenreIds);
        var sorted = _genres
            .Where(genre => !_modelGenreIds.Contains(genre.Id))
            .OrderByDescending(g => selectedGenreIds.Contains(g.Id))
            .ThenByDescending(g => genreCounts.GetValueOrDefault(g.Id, 0))
            .ThenBy(g => g.Name)
            .ToList();

        GenresPanel.Children.Clear();
        _genreChips.Clear();

        foreach (var genre in sorted)
        {
            var count = genreCounts.GetValueOrDefault(genre.Id, 0);
            var btn = MetadataChipFactory.Create(genre.Name, count, selectedGenreIds.Contains(genre.Id));
            btn.IsCheckedChanged += (_, _) =>
            {
                RebuildStyleChips();
                UpdateSaveButton();
            };
            _genreChips.Add((genre, btn));
            GenresPanel.Children.Add(btn);
        }
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

    private void UpdateSaveButton()
    {
        SaveBtn.IsEnabled = _track != null && !string.IsNullOrWhiteSpace(TitleBox.Text);
    }

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

        CloseOverlay();
        TrackSaved?.Invoke();
    }

    private async System.Threading.Tasks.Task AnalyzeImportedTrackAsync(MusicTrack track)
    {
        AnalysisBusyLayer.IsVisible = true;
        StatusText.Text = "Analyzing genres with Discogs-MAEST…";
        SaveBtn.IsEnabled = false;
        var error = await MusicLibraryService.Current.AnalyzeTrackAsync(track);
        AnalysisBusyLayer.IsVisible = false;
        StatusText.Text = error is null
            ? "Analysis complete. Review the metadata and save when ready."
            : $"Analysis needs review: {error}";
        ShowModelPredictions(track, applyMappedGenres: error is null);
        StartModelAnalysisPreview();
        ShowModelSelectedGenres(track);
        RebuildGenreChips(SelectedGenreIds());
        ShowAudioAnalysis(track);
        ShowSoundProfile(track);
        UpdateSaveButton();
    }

    private void ShowAudioAnalysis(MusicTrack track)
    {
        var analysis = MusicLibraryService.Current.GetTrackAudioAnalysis(track.Id);
        AudioAnalysisSection.IsVisible = analysis is not null;
        if (analysis is null) return;

        BpmText.Text = analysis.Bpm is double bpm ? $"{bpm:0.#} BPM" : "—";
        IntegratedLoudnessText.Text = analysis.IntegratedLoudness is double loudness
            ? $"{loudness:0.#} LUFS"
            : "—";
        LoudnessRangeText.Text = analysis.LoudnessRange is double range
            ? $"{range:0.#} LU"
            : "—";

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

        var summary = string.Join("  ·  ", derived.Select(attribute =>
            $"{FormatAttributeName(attribute.Key)}: {attribute.EffectiveValue}"));
        if (summary.Length > 0)
            SoundProfilePanel.Children.Add(new TextBlock { Text = summary, FontSize = 11, FontWeight = Avalonia.Media.FontWeight.SemiBold, Opacity = .85 });

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
            var title = new TextBlock { Text = name, FontSize = 11 };
            ToolTip.SetTip(title, explanation);
            var bar = new ProgressBar { Minimum = 0, Maximum = 1, Value = score.Value, Height = 6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.Parse("#1E9AF0")) };
            var value = new TextBlock { Text = score.Value.ToString("0.##"), FontSize = 10.5, Opacity = .72, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            Grid.SetColumn(bar, 1); Grid.SetColumn(value, 2);
            row.Children.Add(title); row.Children.Add(bar); row.Children.Add(value);
            SoundProfilePanel.Children.Add(row);
        }
    }

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
        < 80 => "Slow tempo.",
        < 120 => "Moderate tempo.",
        < 140 => "Medium-fast tempo.",
        < 175 => "Fast tempo.",
        _ => "Very fast tempo."
    };

    private static string GetIntegratedLoudnessInsight(double lufs) => lufs switch
    {
        >= -8 => "Very loud overall.",
        >= -11 => "Loud overall.",
        >= -14 => "Moderately loud overall.",
        _ => "Relatively quiet overall."
    };

    private static string GetLoudnessRangeInsight(double lu) => lu switch
    {
        <= 3 => "Very even loudness; little contrast between sections.",
        <= 6 => "Controlled dynamics with moderate variation.",
        <= 10 => "Noticeable contrast between quieter and louder sections.",
        _ => "High dynamic contrast across the track."
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
        var assignments = MusicLibraryService.Current.GetTrackModelGenres(track.Id);
        _modelGenreIds = assignments.Select(assignment => assignment.GenreId).ToHashSet();
        ModelSelectedGenresSection.IsVisible = assignments.Count > 0;
        ModelSelectedGenresPanel.Children.Clear();
        foreach (var assignment in assignments)
        {
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
                FontSize = 10.5, Opacity = 0.72, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(divider, 1);
            Grid.SetColumn(reason, 2);
            row.Children.Add(genreName);
            row.Children.Add(divider);
            row.Children.Add(reason);
            container.Child = row;
            var enabled = assignment.IsEnabled;
            ApplyModelGenreVisual(container, genreName, divider, enabled);
            container.PointerPressed += (_, _) =>
            {
                enabled = !enabled;
                MusicLibraryService.Current.SetTrackModelGenreEnabled(track.Id, assignment.GenreId, enabled);
                ApplyModelGenreVisual(container, genreName, divider, enabled);
            };
            ModelSelectedGenresPanel.Children.Add(container);
        }
    }

    private static void ApplyModelGenreVisual(Border container, TextBlock genreName, Border divider, bool enabled)
    {
        container.Background = new SolidColorBrush(Color.Parse(enabled ? "#153B54" : "#23272D"));
        container.BorderBrush = new SolidColorBrush(Color.Parse(enabled ? "#3286B8" : "#3B414A"));
        genreName.Foreground = enabled ? Brushes.White : new SolidColorBrush(Color.Parse("#A3ABB5"));
        divider.Background = new SolidColorBrush(Color.Parse(enabled ? "#4D9AC5" : "#4A535D"));
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => CloseOverlay();

    private void CloseOverlay()
    {
        _analysisPreviewCancellation?.Cancel();
        AnalysisPopup.IsOpen = false;
        IsVisible = false;
        _track = null;
    }
}
