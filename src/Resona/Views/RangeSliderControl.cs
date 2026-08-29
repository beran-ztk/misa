using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Resona.Views;

public sealed class RangeSliderControl : Control
{
    private const double ThumbRadius = 6;
    private const double TrackInset = 7;
    private bool _isDragging;

    public double Value { get; private set; }

    public event Action<double>? ValueChanged;

    public RangeSliderControl()
    {
        MinWidth = 120;
        Height = 26;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public void SetValue(double value, bool notify = false)
    {
        value = Snap(value);
        if (Math.Abs(Value - value) < 0.001)
            return;

        Value = value;
        InvalidateVisual();
        if (notify)
            ValueChanged?.Invoke(Value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Math.Max(0, Bounds.Width - TrackInset * 2);
        var centerY = Bounds.Height / 2;
        var valueX = TrackInset + width * Value / 100;

        context.DrawRectangle(new SolidColorBrush(Color.Parse("#35FFFFFF")), null,
            new Rect(TrackInset, centerY - 2, width, 4));
        var tickPen = new Pen(new SolidColorBrush(Color.Parse("#52FFFFFF")), 1);
        for (var step = 0; step <= 10; step++)
        {
            var tickX = TrackInset + width * step / 10d;
            context.DrawLine(tickPen, new Point(tickX, centerY - 4), new Point(tickX, centerY + 4));
        }
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#B8D8D3C4")), null,
            new Rect(valueX, centerY - 2, Math.Max(0, TrackInset + width - valueX), 4));
        var thumbBrush = new SolidColorBrush(Color.Parse("#F0E8E3D5"));
        var thumbBorder = new Pen(new SolidColorBrush(Color.Parse("#80121212")), 1);
        context.DrawEllipse(thumbBrush, thumbBorder, new Point(valueX, centerY), ThumbRadius, ThumbRadius);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        e.Pointer.Capture(this);
        SetValue(ValueFromPosition(e.GetPosition(this).X), notify: true);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_isDragging || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        SetValue(ValueFromPosition(e.GetPosition(this).X), notify: true);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private double ValueFromPosition(double x)
    {
        var width = Math.Max(1, Bounds.Width - TrackInset * 2);
        return Snap((x - TrackInset) / width * 100);
    }

    private static double Snap(double value) =>
        Math.Clamp(Math.Round(value / 10d, MidpointRounding.AwayFromZero) * 10d, 0d, 100d);
}
