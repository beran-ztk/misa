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
    private List<Language> _languages = [];

    public EditTrackWindow(MusicTrack track)
    {
        InitializeComponent();
        _track = track;
        TitleBox.TextChanged += (_, _) => UpdateSaveButton();
        GenresBox.SelectionChanged += (_, _) => UpdateSaveButton();
        RatingBox.SelectionChanged += (_, _) => UpdateSaveButton();
        LoadAndPrefill();
    }

    private void LoadAndPrefill()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _languages = MusicLibraryService.Current.GetLanguages();

        TitleBox.Text = _track.Title;
        NotesBox.Text = _track.Notes ?? "";
        ReEvalCheckBox.IsChecked = _track.ReEvaluationNeeded;

        GenresBox.ItemsSource = _genres.Select(g => g.Name).ToList();
        var currentGenreIds = MusicLibraryService.Current.GetTrackGenreIds(_track.Id);
        for (int i = 0; i < _genres.Count; i++)
        {
            if (currentGenreIds.Contains(_genres[i].Id))
                GenresBox.Selection.Select(i);
        }

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

        LanguagesBox.ItemsSource = _languages.Select(l => l.Name).ToList();
        var currentLanguageIds = MusicLibraryService.Current.GetTrackLanguageIds(_track.Id);
        for (int i = 0; i < _languages.Count; i++)
        {
            if (currentLanguageIds.Contains(_languages[i].Id))
                LanguagesBox.Selection.Select(i);
        }
    }

    private void UpdateSaveButton()
    {
        SaveBtn.IsEnabled = !string.IsNullOrWhiteSpace(TitleBox.Text)
                           && (GenresBox.SelectedItems?.Count ?? 0) > 0
                           && RatingBox.SelectedIndex > 0;
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text!.Trim();
        var genreIds = GenresBox.SelectedItems?
            .Cast<string>()
            .Select(name => _genres.First(g => g.Name == name).Id)
            .ToList() ?? [];
        var ratingId = _ratings[RatingBox.SelectedIndex - 1].Id;
        var styleIds = StylesBox.SelectedItems?
            .Cast<string>()
            .Select(name => _styles.First(s => s.Name == name).Id)
            .ToList() ?? [];
        var languageIds = LanguagesBox.SelectedItems?
            .Cast<string>()
            .Select(name => _languages.First(l => l.Name == name).Id)
            .ToList() ?? [];
        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        var reEval = ReEvalCheckBox.IsChecked == true;

        MusicLibraryService.Current.UpdateTrack(_track.Id, title, genreIds, ratingId, styleIds, languageIds, notes, reEval);
        Close(true);
    }

    private void OnCancelClicked(object? sender, RoutedEventArgs e) => Close(false);
}
