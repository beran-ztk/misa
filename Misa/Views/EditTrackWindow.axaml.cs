using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Misa.Music.Models;
using Misa.Music.Services;

namespace Misa.Views;

public partial class EditTrackWindow : Window
{
    private readonly MusicTrack _track;
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    public EditTrackWindow(MusicTrack track)
    {
        InitializeComponent();
        _track = track;
        TitleBox.TextChanged += (_, _) => UpdateSaveButton();
        GenreBox.SelectionChanged += (_, _) => UpdateSaveButton();
        RatingBox.SelectionChanged += (_, _) => UpdateSaveButton();
        LoadAndPrefill();
    }

    private void LoadAndPrefill()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();

        TitleBox.Text = _track.Title;

        GenreBox.ItemsSource = new[] { "(Select genre)" }.Concat(_genres.Select(g => g.Name)).ToList();
        var genreIdx = _genres.FindIndex(g => g.Id == _track.GenreId);
        GenreBox.SelectedIndex = genreIdx >= 0 ? genreIdx + 1 : 0;

        RatingBox.ItemsSource = new[] { "(Select rating)" }.Concat(_ratings.Select(r => r.Name)).ToList();
        var ratingIdx = _ratings.FindIndex(r => r.Id == _track.RatingId);
        RatingBox.SelectedIndex = ratingIdx >= 0 ? ratingIdx + 1 : 0;

        StylesBox.ItemsSource = _styles.Select(s => s.Name).ToList();
        var currentStyleIds = MusicLibraryService.Current.GetTrackStyleIds(_track.Id);
        for (int i = 0; i < _styles.Count; i++)
        {
            if (currentStyleIds.Contains(_styles[i].Id))
                StylesBox.Selection.Select(i);
        }
    }

    private void UpdateSaveButton()
    {
        SaveBtn.IsEnabled = !string.IsNullOrWhiteSpace(TitleBox.Text)
                           && GenreBox.SelectedIndex > 0
                           && RatingBox.SelectedIndex > 0;
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text!.Trim();
        var genreId = _genres[GenreBox.SelectedIndex - 1].Id;
        var ratingId = _ratings[RatingBox.SelectedIndex - 1].Id;
        var styleIds = StylesBox.SelectedItems?
            .Cast<string>()
            .Select(name => _styles.First(s => s.Name == name).Id)
            .ToList() ?? [];

        MusicLibraryService.Current.UpdateTrack(_track.Id, title, genreId, ratingId, styleIds);
        Close(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
}
