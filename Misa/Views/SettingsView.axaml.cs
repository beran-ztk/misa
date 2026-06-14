using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Misa.Music.Models;
using Misa.Music.Services;

namespace Misa.Views;

public partial class SettingsView : UserControl
{
    public Action? PrepareForReset;
    public Action? OnResetComplete;
    public Action? OnMetadataChanged;

    private List<Genre> _genres = [];
    private List<Style> _styles = [];
    private List<Rating> _ratings = [];

    public SettingsView()
    {
        InitializeComponent();
        try { LoadAll(); } catch { }
    }

    private void LoadAll()
    {
        LoadGenres();
        LoadStyles();
        LoadRatings();
    }

    // --- Genres ---

    private void LoadGenres()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        GenreList.ItemsSource = _genres.Select(g => g.Name).ToList();
    }

    private void OnGenreSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx >= 0 && idx < _genres.Count)
            GenreInput.Text = _genres[idx].Name;
    }

    private void OnAddGenreClicked(object? sender, RoutedEventArgs e)
    {
        var name = GenreInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { GenreStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddGenre(name);
        GenreInput.Text = "";
        GenreStatus.Text = "Added.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameGenreClicked(object? sender, RoutedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx < 0) { GenreStatus.Text = "Select a genre first."; return; }
        var name = GenreInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { GenreStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameGenre(_genres[idx].Id, name);
        GenreStatus.Text = "Renamed.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteGenreClicked(object? sender, RoutedEventArgs e)
    {
        var idx = GenreList.SelectedIndex;
        if (idx < 0) { GenreStatus.Text = "Select a genre first."; return; }
        var result = MusicLibraryService.Current.TryDeleteGenre(_genres[idx].Id);
        if (!result.Success) { GenreStatus.Text = result.Error; return; }
        GenreStatus.Text = "Deleted.";
        LoadGenres();
        OnMetadataChanged?.Invoke();
    }

    // --- Styles ---

    private void LoadStyles()
    {
        _styles = MusicLibraryService.Current.GetStyles();
        StyleList.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void OnStyleSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx >= 0 && idx < _styles.Count)
            StyleInput.Text = _styles[idx].Name;
    }

    private void OnAddStyleClicked(object? sender, RoutedEventArgs e)
    {
        var name = StyleInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { StyleStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddStyle(name);
        StyleInput.Text = "";
        StyleStatus.Text = "Added.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameStyleClicked(object? sender, RoutedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx < 0) { StyleStatus.Text = "Select a style first."; return; }
        var name = StyleInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { StyleStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameStyle(_styles[idx].Id, name);
        StyleStatus.Text = "Renamed.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteStyleClicked(object? sender, RoutedEventArgs e)
    {
        var idx = StyleList.SelectedIndex;
        if (idx < 0) { StyleStatus.Text = "Select a style first."; return; }
        var result = MusicLibraryService.Current.TryDeleteStyle(_styles[idx].Id);
        if (!result.Success) { StyleStatus.Text = result.Error; return; }
        StyleStatus.Text = "Deleted.";
        LoadStyles();
        OnMetadataChanged?.Invoke();
    }

    // --- Ratings ---

    private void LoadRatings()
    {
        _ratings = MusicLibraryService.Current.GetRatings();
        RatingList.ItemsSource = _ratings.Select(r => r.Name).ToList();
    }

    private void OnRatingSelected(object? sender, SelectionChangedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx >= 0 && idx < _ratings.Count)
            RatingInput.Text = _ratings[idx].Name;
    }

    private void OnAddRatingClicked(object? sender, RoutedEventArgs e)
    {
        var name = RatingInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { RatingStatus.Text = "Enter a name."; return; }
        MusicLibraryService.Current.AddRating(name);
        RatingInput.Text = "";
        RatingStatus.Text = "Added.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }

    private void OnRenameRatingClicked(object? sender, RoutedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx < 0) { RatingStatus.Text = "Select a rating first."; return; }
        var name = RatingInput.Text?.Trim();
        if (string.IsNullOrEmpty(name)) { RatingStatus.Text = "Enter a new name."; return; }
        MusicLibraryService.Current.RenameRating(_ratings[idx].Id, name);
        RatingStatus.Text = "Renamed.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }

    private void OnDeleteRatingClicked(object? sender, RoutedEventArgs e)
    {
        var idx = RatingList.SelectedIndex;
        if (idx < 0) { RatingStatus.Text = "Select a rating first."; return; }
        var result = MusicLibraryService.Current.TryDeleteRating(_ratings[idx].Id);
        if (!result.Success) { RatingStatus.Text = result.Error; return; }
        RatingStatus.Text = "Deleted.";
        LoadRatings();
        OnMetadataChanged?.Invoke();
    }
}
