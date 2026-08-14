using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Resona.Models;

namespace Resona.Views;

/// <summary>
/// Displays each subgenre with the stable color of its main genre.
/// </summary>
public sealed class TrackGenreSummaryControl : Panel
{
    public static readonly StyledProperty<IReadOnlyList<TrackGenreDisplay>?> GenresProperty =
        AvaloniaProperty.Register<TrackGenreSummaryControl, IReadOnlyList<TrackGenreDisplay>?>(nameof(Genres));

    private readonly List<TextBlock> _genreLabels = [];
    private readonly TextBlock _moreLabel = new()
    {
        FontSize = 10,
        FontWeight = FontWeight.SemiBold,
        Foreground = ThemeResources.Brush("Theme.Brush.TextMuted"),
        Opacity = 0.75,
        Margin = new Thickness(5, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    public IReadOnlyList<TrackGenreDisplay>? Genres
    {
        get => GetValue(GenresProperty);
        set => SetValue(GenresProperty, value);
    }

    static TrackGenreSummaryControl()
    {
        GenresProperty.Changed.AddClassHandler<TrackGenreSummaryControl>((control, _) => control.Rebuild());
    }

    public TrackGenreSummaryControl()
    {
        ClipToBounds = true;
        Children.Add(_moreLabel);
    }

    private void Rebuild()
    {
        Children.Clear();
        _genreLabels.Clear();

        var genres = Genres ?? [];
        for (var index = 0; index < genres.Count; index++)
        {
            var genre = genres[index];
            var separator = index switch
            {
                0 => string.Empty,
                _ when index == genres.Count - 1 => " and ",
                _ => ", "
            };
            var label = new TextBlock
            {
                Text = separator + genre.Name,
                Foreground = genre.Foreground,
                FontSize = 11,
                FontWeight = FontWeight.Medium,
                Opacity = 0.9,
                VerticalAlignment = VerticalAlignment.Center
            };
            _genreLabels.Add(label);
            Children.Add(label);
        }

        Children.Add(_moreLabel);
        InvalidateMeasure();
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

        var desiredWidth = _genreLabels.Sum(label => label.DesiredSize.Width);
        var desiredHeight = _genreLabels.Count == 0 ? 0 : _genreLabels.Max(label => label.DesiredSize.Height);
        return new Size(
            double.IsInfinity(availableSize.Width) ? desiredWidth : Math.Min(desiredWidth, availableSize.Width),
            desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var genres = Genres ?? [];
        if (genres.Count == 0)
        {
            _moreLabel.IsVisible = false;
            return finalSize;
        }

        var count = genres.Count;
        while (count > 0 && !Fits(count, genres.Count - count, finalSize.Width))
            count--;

        _moreLabel.IsVisible = count < genres.Count && Fits(count, genres.Count - count, finalSize.Width);

        var x = 0d;
        for (var index = 0; index < _genreLabels.Count; index++)
        {
            var label = _genreLabels[index];
            label.IsVisible = index < count;
            if (!label.IsVisible)
                continue;

            var y = Math.Max(0, (finalSize.Height - label.DesiredSize.Height) / 2);
            label.Arrange(new Rect(x, y, label.DesiredSize.Width, label.DesiredSize.Height));
            x += label.DesiredSize.Width;
        }

        if (_moreLabel.IsVisible)
        {
            _moreLabel.Text = $"+{genres.Count - count}";
            _moreLabel.Measure(new Size(double.PositiveInfinity, finalSize.Height));
            var y = Math.Max(0, (finalSize.Height - _moreLabel.DesiredSize.Height) / 2);
            _moreLabel.Arrange(new Rect(x, y, _moreLabel.DesiredSize.Width, _moreLabel.DesiredSize.Height));
        }

        return finalSize;
    }

    private bool Fits(int shownGenreCount, int hiddenGenreCount, double availableWidth)
    {
        var width = _genreLabels.Take(shownGenreCount).Sum(label => label.DesiredSize.Width);
        if (hiddenGenreCount <= 0)
            return width <= availableWidth;

        _moreLabel.Text = $"+{hiddenGenreCount}";
        _moreLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return width + _moreLabel.DesiredSize.Width <= availableWidth;
    }
}
