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

    private readonly List<TextBlock> _tagLabels = [];
    private readonly TextBlock _moreLabel = new()
    {
        FontSize = 10.5,
        Opacity = .5,
        VerticalAlignment = VerticalAlignment.Center
    };

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
        Children.Add(_moreLabel);
    }

    private void Rebuild()
    {
        Children.Clear();
        _tagLabels.Clear();

        foreach (var tag in Tags ?? [])
        {
            var label = new TextBlock
            {
                Text = tag.Name,
                Foreground = tag.Foreground,
                FontSize = 10.5,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            _tagLabels.Add(label);
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

        return new Size(0, _tagLabels.Count == 0 ? 0 : _tagLabels.Max(label => label.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var tags = Tags ?? [];
        if (tags.Count == 0)
        {
            _moreLabel.IsVisible = false;
            return finalSize;
        }

        var count = tags.Count;
        while (count > 0 && !Fits(count, tags.Count - count, finalSize.Width))
            count--;

        if (count == 0 && !Fits(0, tags.Count, finalSize.Width))
            _moreLabel.IsVisible = false;
        else
            _moreLabel.IsVisible = count < tags.Count;

        var x = 0d;
        for (var index = 0; index < _tagLabels.Count; index++)
        {
            var label = _tagLabels[index];
            label.IsVisible = index < count;
            if (!label.IsVisible) continue;
            label.Arrange(new Rect(x, 0, label.DesiredSize.Width, finalSize.Height));
            x += label.DesiredSize.Width;
        }

        if (_moreLabel.IsVisible)
        {
            _moreLabel.Text = $"+{tags.Count - count}";
            _moreLabel.Measure(new Size(double.PositiveInfinity, finalSize.Height));
            _moreLabel.Arrange(new Rect(x, 0, _moreLabel.DesiredSize.Width, finalSize.Height));
        }

        return finalSize;
    }

    private bool Fits(int shownTagCount, int hiddenTagCount, double availableWidth)
    {
        var width = _tagLabels.Take(shownTagCount).Sum(label => label.DesiredSize.Width);
        if (hiddenTagCount <= 0)
            return width <= availableWidth;

        _moreLabel.Text = $"+{hiddenTagCount}";
        _moreLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return width + _moreLabel.DesiredSize.Width <= availableWidth;
    }
}
