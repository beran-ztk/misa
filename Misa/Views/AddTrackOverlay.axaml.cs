using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Misa.Music.Models;
using Misa.Music.Services;

namespace Misa.Views;

public partial class AddTrackOverlay : UserControl
{
    private List<Genre> _genres = [];
    private List<Rating> _ratings = [];
    private List<Style> _styles = [];
    private List<Language> _languages = [];

    private readonly List<ToggleButton> _genreButtons = [];
    private readonly List<ToggleButton> _styleButtons = [];
    private readonly List<ToggleButton> _languageButtons = [];

    private bool _downloading;

    public event Action? TrackDownloaded;
    public event Action? CloseRequested;

    public AddTrackOverlay()
    {
        InitializeComponent();
        UrlBox.TextChanged += (_, _) => UpdateDownloadButton();
        RatingBox.SelectionChanged += (_, _) => UpdateDownloadButton();
    }

    public void Open()
    {
        _downloading = false;
        LoadLookups();
        ClearForm();
        IsVisible = true;
    }

    private void LoadLookups()
    {
        _genres = MusicLibraryService.Current.GetGenres();
        _ratings = MusicLibraryService.Current.GetRatings();
        _styles = MusicLibraryService.Current.GetStyles();
        _languages = MusicLibraryService.Current.GetLanguages();

        AddChips(GenresPanel, _genres.Select(g => g.Name), _genreButtons);
        AddChips(StylesPanel, _styles.Select(s => s.Name), _styleButtons);
        AddChips(LanguagesPanel, _languages.Select(l => l.Name), _languageButtons);

        RatingBox.ItemsSource = new[] { "(Select rating)" }.Concat(_ratings.Select(r => r.Name)).ToList();
        RatingBox.SelectedIndex = 0;
    }

    private void ClearForm()
    {
        UrlBox.Text = "";
        TitleBox.Text = "";
        StatusText.Text = "";
        CloseBtn.IsEnabled = true;
        DownloadBtn.IsEnabled = false;
        foreach (var b in _genreButtons) b.IsChecked = false;
        foreach (var b in _styleButtons) b.IsChecked = false;
        foreach (var b in _languageButtons) b.IsChecked = false;
        RatingBox.SelectedIndex = 0;
    }

    private void AddChips(WrapPanel panel, IEnumerable<string> names, List<ToggleButton> buttons)
    {
        panel.Children.Clear();
        buttons.Clear();
        foreach (var name in names.OrderBy(n => n))
        {
            var btn = new ToggleButton
            {
                Content = name,
                Padding = new Thickness(12, 6),
                Margin = new Thickness(0, 0, 6, 6),
                CornerRadius = new CornerRadius(12),
            };
            btn.IsCheckedChanged += (_, _) => UpdateDownloadButton();
            panel.Children.Add(btn);
            buttons.Add(btn);
        }
    }

    private void UpdateDownloadButton()
    {
        DownloadBtn.IsEnabled = !string.IsNullOrWhiteSpace(UrlBox.Text)
                               && _genreButtons.Any(b => b.IsChecked == true)
                               && RatingBox.SelectedIndex > 0;
    }

    private async void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        _downloading = true;
        DownloadBtn.IsEnabled = false;
        CloseBtn.IsEnabled = false;
        StatusText.Text = "Downloading…";

        var request = new DownloadRequest
        {
            RawUrl = UrlBox.Text?.Trim() ?? "",
            CustomTitle = string.IsNullOrWhiteSpace(TitleBox.Text) ? null : TitleBox.Text.Trim(),
            GenreIds = _genreButtons
                .Where(b => b.IsChecked == true)
                .Select(b => _genres.First(g => g.Name == (string)b.Content!).Id)
                .ToList(),
            RatingId = _ratings[RatingBox.SelectedIndex - 1].Id,
            StyleIds = _styleButtons
                .Where(b => b.IsChecked == true)
                .Select(b => _styles.First(s => s.Name == (string)b.Content!).Id)
                .ToList(),
            LanguageIds = _languageButtons
                .Where(b => b.IsChecked == true)
                .Select(b => _languages.First(l => l.Name == (string)b.Content!).Id)
                .ToList(),
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
