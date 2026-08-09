using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Music.Views;

/// <summary>
/// Renders an artwork like UniformToFill, but allows choosing the horizontal
/// focal point of the source image instead of always cropping around its center.
/// </summary>
public sealed class FocusedArtworkImage : Control
{
    private IImage? _source;
    private double _focusX = 0.5;

    public IImage? Source
    {
        get => _source;
        set
        {
            if (ReferenceEquals(_source, value))
                return;

            _source = value;
            InvalidateVisual();
        }
    }

    public double FocusX
    {
        get => _focusX;
        set
        {
            var clamped = Math.Clamp(value, 0, 1);
            if (Math.Abs(_focusX - clamped) < 0.0001)
                return;

            _focusX = clamped;
            InvalidateVisual();
        }
    }

    public bool IsHorizontallyCropped
    {
        get
        {
            var source = Source;
            return source is not null
                   && Bounds.Width > 0
                   && Bounds.Height > 0
                   && source.Size.Width / source.Size.Height > Bounds.Width / Bounds.Height;
        }
    }

    public override void Render(DrawingContext context)
    {
        var source = Source;
        if (source is null || Bounds.Width <= 0 || Bounds.Height <= 0
                           || source.Size.Width <= 0 || source.Size.Height <= 0)
            return;

        var sourceWidth = source.Size.Width;
        var sourceHeight = source.Size.Height;
        var targetAspect = Bounds.Width / Bounds.Height;
        var sourceAspect = sourceWidth / sourceHeight;
        Rect sourceRect;

        if (sourceAspect > targetAspect)
        {
            var visibleWidth = sourceHeight * targetAspect;
            var availableOffset = sourceWidth - visibleWidth;
            sourceRect = new Rect(availableOffset * FocusX, 0, visibleWidth, sourceHeight);
        }
        else
        {
            var visibleHeight = sourceWidth / targetAspect;
            sourceRect = new Rect(0, (sourceHeight - visibleHeight) / 2, sourceWidth, visibleHeight);
        }

        context.DrawImage(source, sourceRect, new Rect(Bounds.Size));
    }
}
