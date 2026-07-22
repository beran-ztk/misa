using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Music.Models;

namespace Music.Views;

/// <summary>
/// A single-line tag row that shows every tag which fits and summarizes the remainder as "+N".
/// </summary>
public sealed class TrackTagSummaryControl : Panel
{
    public static readonly StyledProperty<IReadOnlyList<TrackTagDisplay>?> TagsProperty =
        AvaloniaProperty.Register<TrackTagSummaryControl, IReadOnlyList<TrackTagDisplay>?>(nameof(Tags));

    private readonly List<Border> _tagChips = [];
    private readonly TextBlock _moreLabel = new()
    {
        FontSize = 9.5,
        FontWeight = FontWeight.SemiBold,
        Foreground = new SolidColorBrush(Color.Parse("#C2D9A5")),
        VerticalAlignment = VerticalAlignment.Center
    };
    private readonly Border _moreChip;

    public IReadOnlyList<TrackTagDisplay>? Tags
    {
        get => GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    static TrackTagSummaryControl()
    {
        TagsProperty.Changed.AddClassHandler<TrackTagSummaryControl>((control, _) => control.Rebuild());
    }

    public TrackTagSummaryControl()
    {
        ClipToBounds = true;
        VerticalAlignment = VerticalAlignment.Center;
        _moreChip = CreateChip(_moreLabel);
        Children.Add(_moreChip);
    }

    private void Rebuild()
    {
        Children.Clear();
        _tagChips.Clear();

        foreach (var tag in Tags ?? [])
        {
            var label = new TextBlock
            {
                Text = tag.Name,
                Foreground = tag.Foreground,
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            var chip = CreateChip(label);
            _tagChips.Add(chip);
            Children.Add(chip);
        }

        Children.Add(_moreChip);
        InvalidateMeasure();
        InvalidateArrange();
    }

    private static Border CreateChip(Control content) => new()
    {
        Height = 18,
        Padding = new Thickness(6, 0),
        Margin = new Thickness(0, 0, 6, 0),
        CornerRadius = new CornerRadius(8),
        Background = new SolidColorBrush(Color.Parse("#202B1E")),
        BorderBrush = new SolidColorBrush(Color.Parse("#536746")),
        BorderThickness = new Thickness(1),
        VerticalAlignment = VerticalAlignment.Center,
        Child = content
    };

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

        return new Size(0, _tagChips.Count == 0 ? 0 : _tagChips.Max(chip => chip.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var tags = Tags ?? [];
        if (tags.Count == 0)
        {
            _moreChip.IsVisible = false;
            return finalSize;
        }

        var count = tags.Count;
        while (count > 0 && !Fits(count, tags.Count - count, finalSize.Width))
            count--;

        if (count == 0 && !Fits(0, tags.Count, finalSize.Width))
            _moreChip.IsVisible = false;
        else
            _moreChip.IsVisible = count < tags.Count;

        var x = 0d;
        for (var index = 0; index < _tagChips.Count; index++)
        {
            var chip = _tagChips[index];
            chip.IsVisible = index < count;
            if (!chip.IsVisible) continue;
            var y = Math.Max(0, (finalSize.Height - chip.DesiredSize.Height) / 2);
            chip.Arrange(new Rect(x, y, chip.DesiredSize.Width, chip.DesiredSize.Height));
            x += chip.DesiredSize.Width;
        }

        if (_moreChip.IsVisible)
        {
            _moreLabel.Text = $"+{tags.Count - count}";
            _moreChip.Measure(new Size(double.PositiveInfinity, finalSize.Height));
            var y = Math.Max(0, (finalSize.Height - _moreChip.DesiredSize.Height) / 2);
            _moreChip.Arrange(new Rect(x, y, _moreChip.DesiredSize.Width, _moreChip.DesiredSize.Height));
        }

        return finalSize;
    }

    private bool Fits(int shownTagCount, int hiddenTagCount, double availableWidth)
    {
        var width = _tagChips.Take(shownTagCount).Sum(chip => chip.DesiredSize.Width);
        if (hiddenTagCount <= 0)
            return width <= availableWidth;

        _moreLabel.Text = $"+{hiddenTagCount}";
        _moreChip.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return width + _moreChip.DesiredSize.Width <= availableWidth;
    }
}
