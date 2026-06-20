using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
        ShowModelSelectedGenres(track);
        RebuildGenreChips(SelectedGenreIds());
        ShowAudioAnalysis(track);
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
    }

    private void ShowModelPredictions(MusicTrack track, bool applyMappedGenres)
    {
        const double displayThreshold = 0.1;
        var predictions = MusicLibraryService.Current.GetTrackGenrePredictions(track.Id)
            .Where(prediction => prediction.Score > displayThreshold)
            .ToList();

        ModelAnalysisSection.IsVisible = predictions.Count > 0;
        ModelPredictionsText.Text = predictions.Count == 0
            ? string.Empty
            : string.Join("\n", predictions.Select(prediction =>
                $"{prediction.ModelGenreName} → {prediction.ModelSubgenreName}  ({prediction.Score:0.###})"));

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
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            var chip = new ToggleButton
            {
                Content = assignment.GenreName,
                IsChecked = assignment.IsEnabled,
                Padding = new Avalonia.Thickness(10, 5),
                CornerRadius = new Avalonia.CornerRadius(13),
                FontSize = 11
            };
            var reason = new TextBlock
            {
                Text = string.Join(" · ", assignment.Reasons.Select(item => $"{item.ModelGenreName} → {item.ModelSubgenreName} ({item.Score:0.###})")),
                FontSize = 10.5, Opacity = 0.72, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Avalonia.Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(reason, 1);
            row.Children.Add(chip);
            row.Children.Add(reason);
            container.Child = row;
            ApplyModelGenreVisual(container, chip, assignment.IsEnabled);
            chip.IsCheckedChanged += (_, _) =>
            {
                var enabled = chip.IsChecked == true;
                MusicLibraryService.Current.SetTrackModelGenreEnabled(track.Id, assignment.GenreId, enabled);
                ApplyModelGenreVisual(container, chip, enabled);
            };
            ModelSelectedGenresPanel.Children.Add(container);
        }
    }

    private static void ApplyModelGenreVisual(Border container, ToggleButton chip, bool enabled)
    {
        container.Background = new SolidColorBrush(Color.Parse(enabled ? "#153B54" : "#23272D"));
        container.BorderBrush = new SolidColorBrush(Color.Parse(enabled ? "#3286B8" : "#3B414A"));
        chip.Background = new SolidColorBrush(Color.Parse(enabled ? "#176EA8" : "#30353C"));
        chip.BorderBrush = new SolidColorBrush(Color.Parse(enabled ? "#57C2F5" : "#525B66"));
        chip.Foreground = enabled ? Brushes.White : new SolidColorBrush(Color.Parse("#A3ABB5"));
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => CloseOverlay();

    private void CloseOverlay()
    {
        IsVisible = false;
        _track = null;
    }
}
