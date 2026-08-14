using System;
using Avalonia;
using Avalonia.Controls;

namespace Resona.Views;

/// <summary>
/// Keeps tags directly beside the title while still giving the title a finite
/// arrange width so TextTrimming can react to the available row width.
/// </summary>
public sealed class TrackTitleTagPanel : Panel
{
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<TrackTitleTagPanel, double>(nameof(Gap), 6);

    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children)
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));

        if (Children.Count == 0)
            return default;

        var desiredWidth = Children[0].DesiredSize.Width;
        if (Children.Count > 1 && Children[1].DesiredSize.Width > 0)
            desiredWidth += Gap + Children[1].DesiredSize.Width;
        var desiredHeight = 0d;
        foreach (var child in Children)
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);

        return new Size(
            double.IsInfinity(availableSize.Width) ? desiredWidth : Math.Min(desiredWidth, availableSize.Width),
            desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0)
            return finalSize;

        var title = Children[0];
        if (Children.Count == 1)
        {
            ArrangeCentered(title, 0, Math.Min(title.DesiredSize.Width, finalSize.Width), finalSize.Height);
            return finalSize;
        }

        var tags = Children[1];
        var hasTags = tags.DesiredSize.Width > 0;
        var gap = hasTags ? Gap : 0;
        var tagWidth = Math.Min(tags.DesiredSize.Width, Math.Max(0, finalSize.Width - gap));
        var combinedWidth = title.DesiredSize.Width + gap + tagWidth;
        var titleWidth = combinedWidth <= finalSize.Width
            ? title.DesiredSize.Width
            : Math.Max(0, finalSize.Width - gap - tagWidth);

        ArrangeCentered(title, 0, titleWidth, finalSize.Height);
        if (hasTags)
            ArrangeCentered(tags, titleWidth + gap, tagWidth, finalSize.Height);
        return finalSize;
    }

    private static void ArrangeCentered(Control control, double x, double width, double availableHeight)
    {
        var height = Math.Min(control.DesiredSize.Height, availableHeight);
        var y = Math.Max(0, (availableHeight - height) / 2);
        control.Arrange(new Rect(x, y, width, height));
    }
}
