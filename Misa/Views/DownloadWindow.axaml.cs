using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Misa.Music.Models;
using Misa.Music.Services;

namespace Misa.Views;

public partial class DownloadWindow : Window
{
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];

    public DownloadWindow()
    {
        InitializeComponent();
        UrlBox.TextChanged += (_, _) => UpdateDownloadButton();
        GenreBox.SelectionChanged += (_, _) => UpdateDownloadButton();
        RatingBox.SelectionChanged += (_, _) => UpdateDownloadButton();
        LoadLookups();
    }

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();

        GenreBox.ItemsSource = new[] { "(Select genre)" }.Concat(_genres.Select(g => g.Name)).ToList();
        GenreBox.SelectedIndex = 0;
        RatingBox.ItemsSource = new[] { "(Select rating)" }.Concat(_ratings.Select(r => r.Name)).ToList();
        RatingBox.SelectedIndex = 0;
        StylesBox.ItemsSource = _styles.Select(s => s.Name).ToList();
    }

    private void UpdateDownloadButton()
    {
        DownloadBtn.IsEnabled = !string.IsNullOrWhiteSpace(UrlBox.Text)
                               && GenreBox.SelectedIndex > 0
                               && RatingBox.SelectedIndex > 0;
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        DownloadBtn.IsEnabled = false;
        CloseBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var request = new DownloadRequest
        {
            RawUrl = UrlBox.Text?.Trim() ?? "",
            CustomTitle = string.IsNullOrWhiteSpace(TitleBox.Text) ? null : TitleBox.Text.Trim(),
            GenreId = _genres[GenreBox.SelectedIndex - 1].Id,
            RatingId = _ratings[RatingBox.SelectedIndex - 1].Id,
            StyleIds = StylesBox.SelectedItems?
                .Cast<string>()
                .Select(name => _styles.First(s => s.Name == name).Id)
                .ToList() ?? [],
        };

        var result = await MusicLibraryService.Current.DownloadTrackAsync(request);

        if (!result.Success)
        {
            StatusText.Text = result.Error;
            UpdateDownloadButton();
            CloseBtn.IsEnabled = true;
            return;
        }

        Close(true);
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close(false);
}
