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
        for (var i = 1; i < Children.Count; i++)
            if (Children[i].IsVisible && Children[i].DesiredSize.Width > 0)
                desiredWidth += Gap + Children[i].DesiredSize.Width;
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

        var metadataWidth = 0d;
        for (var i = 1; i < Children.Count; i++)
            if (Children[i].IsVisible && Children[i].DesiredSize.Width > 0)
                metadataWidth += Gap + Children[i].DesiredSize.Width;
        var titleWidth = Math.Min(title.DesiredSize.Width,
            Math.Max(Math.Min(100, finalSize.Width), finalSize.Width - metadataWidth));
        ArrangeCentered(title, 0, titleWidth, finalSize.Height);
        var x = titleWidth;
        for (var i = 1; i < Children.Count; i++)
        {
            var child = Children[i];
            if (!child.IsVisible || child.DesiredSize.Width <= 0) continue;
            x = Math.Min(finalSize.Width, x + Gap);
            var width = Math.Min(child.DesiredSize.Width, Math.Max(0, finalSize.Width - x));
            ArrangeCentered(child, x, width, finalSize.Height);
            x += width;
        }
        return finalSize;
    }

    private static void ArrangeCentered(Control control, double x, double width, double availableHeight)
    {
        var height = Math.Min(control.DesiredSize.Height, availableHeight);
        var y = Math.Max(0, (availableHeight - height) / 2);
        control.Arrange(new Rect(x, y, width, height));
    }
}
