using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Music.Models;
using Music.Services;

namespace Music.Views;

public partial class AddTrackOverlay : UserControl
{
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    // Each chip tuple pairs the domain object with its ToggleButton so we can read
    // the selection by ID rather than by parsing button content (which includes counts).
    private readonly List<(Genre Genre, ToggleButton Btn)> _genreChips = [];
    private readonly List<(Style Style, ToggleButton Btn)> _styleChips = [];

    private Dictionary<int, List<int>> _allTrackGenreIds = [];
    private Dictionary<int, List<int>> _allTrackStyleIds = [];

    private bool _downloading;
    private bool _showAllGenres;
    private const int InitialGenreLimit = 10;

    public event Action? TrackDownloaded;
    public event Action? CloseRequested;

    public AddTrackOverlay()
    {
        InitializeComponent();
        BodyScroll.PropertyChanged += (_, _) => UpdateBodyWidth();
        UrlBox.TextChanged += (_, _) => UpdateDownloadButton();
        RatingBox.SelectionChanged += (_, _) => UpdateDownloadButton();
    }

    public void Open()
    {
        _downloading = false;
        _showAllGenres = false;
        LoadLookups();
        ClearForm();
        IsVisible = true;
        UpdateBodyWidth();
    }

    private void UpdateBodyWidth()
    {
        var width = BodyScroll.Bounds.Width - BodyScroll.Padding.Left - BodyScroll.Padding.Right;
        if (width > 0)
            BodyContent.Width = width;
    }

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _allTrackGenreIds = MusicLibraryService.Current.GetAllTrackGenreIds();
        _allTrackStyleIds = MusicLibraryService.Current.GetAllTrackStyleIds();

        RebuildGenreChips();

        RatingBox.ItemsSource = _ratings.Select(r => r.Name).ToList();
        RatingBox.SelectedIndex = -1;
    }

    private void ClearForm()
    {
        UrlBox.Text = "";
        StatusText.Text = "";
        CloseBtn.IsEnabled = true;
        DownloadBtn.IsEnabled = false;
        foreach (var (_, btn) in _genreChips) btn.IsChecked = false;
        StylesPanel.Children.Clear();
        _styleChips.Clear();
        StylesSection.IsVisible = false;
        RatingBox.SelectedIndex = -1;
    }

    // ─── Genre chips ─────────────────────────────────────────────────────────

    private void RebuildGenreChips()
    {
        var genreCounts = MetadataCountService.GenreCounts(_allTrackGenreIds);
        var sorted = _genres
            .OrderByDescending(g => genreCounts.GetValueOrDefault(g.Id, 0))
            .ThenBy(g => g.Name)
            .ToList();

        GenresPanel.Children.Clear();
        _genreChips.Clear();

        for (int i = 0; i < sorted.Count; i++)
        {
            var genre = sorted[i];
            var count = genreCounts.GetValueOrDefault(genre.Id, 0);
            var btn = MetadataChipFactory.Create(genre.Name, count);
            btn.IsVisible = _showAllGenres || i < InitialGenreLimit;
            btn.IsCheckedChanged += (_, _) => OnGenreSelectionChanged();
            _genreChips.Add((genre, btn));
            GenresPanel.Children.Add(btn);
        }

        ShowMoreGenresBtn.IsVisible = sorted.Count > InitialGenreLimit && !_showAllGenres;
    }

    private void OnShowMoreGenresClicked(object? sender, RoutedEventArgs e)
    {
        _showAllGenres = true;
        for (int i = InitialGenreLimit; i < _genreChips.Count; i++)
            _genreChips[i].Btn.IsVisible = true;
        ShowMoreGenresBtn.IsVisible = false;
    }

    // ─── Style chips (rebuilt whenever genre selection changes) ───────────────

    private void OnGenreSelectionChanged()
    {
        UpdateDownloadButton();
        RebuildStyleChips();
    }

    private void RebuildStyleChips()
    {
        var selectedGenreIds = _genreChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Genre.Id)
            .ToHashSet();

        StylesSection.IsVisible = selectedGenreIds.Count > 0;

        if (selectedGenreIds.Count == 0)
        {
            StylesPanel.Children.Clear();
            _styleChips.Clear();
            return;
        }

        // Counts are scoped to tracks that match ALL selected genres (AND logic).
        var styleCounts = MetadataCountService.StyleCountsForGenres(
            _allTrackGenreIds, _allTrackStyleIds, selectedGenreIds);

        // Preserve which styles the user had already checked before the rebuild.
        var prevSelected = _styleChips
            .Where(c => c.Btn.IsChecked == true)
            .Select(c => c.Style.Id)
            .ToHashSet();

        var sorted = _styles
            .OrderByDescending(s => styleCounts.GetValueOrDefault(s.Id, 0))
            .ThenBy(s => s.Name)
            .ToList();

        StylesPanel.Children.Clear();
        _styleChips.Clear();

        foreach (var style in sorted)
        {
            var count = styleCounts.GetValueOrDefault(style.Id, 0);
            var btn = MetadataChipFactory.Create(style.Name, count, prevSelected.Contains(style.Id));
            btn.Opacity = count > 0 ? 1.0 : 0.48;
            btn.IsCheckedChanged += (_, _) => UpdateDownloadButton();
            _styleChips.Add((style, btn));
            StylesPanel.Children.Add(btn);
        }
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    private void UpdateDownloadButton()
    {
        DownloadBtn.IsEnabled = !string.IsNullOrWhiteSpace(UrlBox.Text)
                               && _genreChips.Any(c => c.Btn.IsChecked == true)
                               && RatingBox.SelectedIndex >= 0;
    }

    // ─── Download ─────────────────────────────────────────────────────────────

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        _downloading = true;
        DownloadBtn.IsEnabled = false;
        CloseBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var request = new DownloadRequest
        {
            RawUrl = UrlBox.Text?.Trim() ?? "",
            GenreIds = _genreChips
                .Where(c => c.Btn.IsChecked == true)
                .Select(c => c.Genre.Id)
                .ToList(),
            RatingId = _ratings[RatingBox.SelectedIndex].Id,
            StyleIds = _styleChips
                .Where(c => c.Btn.IsChecked == true)
                .Select(c => c.Style.Id)
                .ToList()
        };

        var result = await MusicLibraryService.Current.DownloadTrackAsync(request);

        _downloading = false;

        if (!result.Success)
        {
            StatusText.Text = result.Error;
            UpdateDownloadButton();
            CloseBtn.IsEnabled = true;
            return;
        }

        TrackDownloaded?.Invoke();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (_downloading) return;
        CloseRequested?.Invoke();
    }
}
