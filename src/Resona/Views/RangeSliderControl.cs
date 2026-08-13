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
    private RangeThumb _activeThumb;

    public double LowerValue { get; private set; }
    public double UpperValue { get; private set; } = 100;

    public event Action<double, double>? ValuesChanged;

    public RangeSliderControl()
    {
        MinWidth = 120;
        Height = 26;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public void SetValues(double lower, double upper, bool notify = false)
    {
        lower = Math.Clamp(lower, 0, 100);
        upper = Math.Clamp(upper, lower, 100);
        if (Math.Abs(LowerValue - lower) < 0.001 && Math.Abs(UpperValue - upper) < 0.001)
            return;

        LowerValue = lower;
        UpperValue = upper;
        InvalidateVisual();
        if (notify)
            ValuesChanged?.Invoke(LowerValue, UpperValue);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Math.Max(0, Bounds.Width - TrackInset * 2);
        var centerY = Bounds.Height / 2;
        var lowerX = TrackInset + width * LowerValue / 100;
        var upperX = TrackInset + width * UpperValue / 100;

        context.DrawRectangle(new SolidColorBrush(Color.Parse("#35FFFFFF")), null,
            new Rect(TrackInset, centerY - 2, width, 4));
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#B8D8D3C4")), null,
            new Rect(lowerX, centerY - 2, Math.Max(0, upperX - lowerX), 4));
        var thumbBrush = new SolidColorBrush(Color.Parse("#F0E8E3D5"));
        var thumbBorder = new Pen(new SolidColorBrush(Color.Parse("#80121212")), 1);
        context.DrawEllipse(thumbBrush, thumbBorder, new Point(lowerX, centerY), ThumbRadius, ThumbRadius);
        context.DrawEllipse(thumbBrush, thumbBorder, new Point(upperX, centerY), ThumbRadius, ThumbRadius);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        var value = ValueFromPosition(e.GetPosition(this).X);
        _activeThumb = Math.Abs(LowerValue - UpperValue) < 0.001
            ? value >= UpperValue ? RangeThumb.Upper : RangeThumb.Lower
            : Math.Abs(value - LowerValue) <= Math.Abs(value - UpperValue)
                ? RangeThumb.Lower
                : RangeThumb.Upper;
        e.Pointer.Capture(this);
        UpdateActiveThumb(value);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_activeThumb == RangeThumb.None || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        UpdateActiveThumb(ValueFromPosition(e.GetPosition(this).X));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _activeThumb = RangeThumb.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private double ValueFromPosition(double x)
    {
        var width = Math.Max(1, Bounds.Width - TrackInset * 2);
        return Math.Clamp((x - TrackInset) / width * 100, 0, 100);
    }

    private void UpdateActiveThumb(double value)
    {
        if (_activeThumb == RangeThumb.Lower)
            SetValues(Math.Min(value, UpperValue), UpperValue, notify: true);
        else if (_activeThumb == RangeThumb.Upper)
            SetValues(LowerValue, Math.Max(value, LowerValue), notify: true);
    }

    private enum RangeThumb
    {
        None,
        Lower,
        Upper
    }
}
