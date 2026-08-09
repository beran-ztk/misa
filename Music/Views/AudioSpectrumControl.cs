using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Music.Views;

public sealed class AudioSpectrumControl : Control
{
    private const int BandCount = 48;
    private readonly float[] _current = new float[BandCount];
    private readonly float[] _target = new float[BandCount];
    private Color _primary = Color.Parse("#5865B8");
    private Color _secondary = Color.Parse("#8051AE");

    public double Sensitivity { get; set; } = 1;
    public double Smoothing { get; set; } = 0.65;

    public bool IsAtRest
    {
        get
        {
            for (var i = 0; i < BandCount; i++)
                if (_current[i] > 0.002 || _target[i] > 0.002)
                    return false;
            return true;
        }
    }

    public void SetColors(Color primary, Color secondary)
    {
        if (_primary == primary && _secondary == secondary)
            return;
        _primary = primary;
        _secondary = secondary;
        InvalidateVisual();
    }

    public void SetSpectrum(IReadOnlyList<float>? spectrum)
    {
        for (var band = 0; band < BandCount; band++)
            _target[band] = spectrum is not null && band < spectrum.Count
                ? Math.Clamp(spectrum[band], 0, 1)
                : 0;
    }

    public void Advance()
    {
        var attack = 0.48 - Math.Clamp(Smoothing, 0, 0.95) * 0.37;
        var release = attack * 0.58;
        var changed = false;
        for (var band = 0; band < BandCount; band++)
        {
            var amount = _target[band] >= _current[band] ? attack : release;
            var next = (float)(_current[band] + (_target[band] - _current[band]) * amount);
            if (Math.Abs(next - _current[band]) > 0.0001)
                changed = true;
            _current[band] = next < 0.001 ? 0 : next;
        }

        if (changed)
            InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 1 || height <= 1)
            return;

        var points = new Point[BandCount];
        var sensitivity = Math.Clamp(Sensitivity, 0.25, 2.5);
        for (var band = 0; band < BandCount; band++)
        {
            var x = band * width / (BandCount - 1);
            var value = Math.Clamp(_current[band] * sensitivity, 0, 1);
            points[band] = new Point(x, height - value * height);
        }

        var geometry = new StreamGeometry();
        using (var stream = geometry.Open())
        {
            stream.BeginFigure(new Point(0, height), true);
            stream.LineTo(points[0]);
            for (var band = 1; band < BandCount - 1; band++)
            {
                var midpoint = new Point(
                    (points[band].X + points[band + 1].X) / 2,
                    (points[band].Y + points[band + 1].Y) / 2);
                stream.QuadraticBezierTo(points[band], midpoint);
            }
            stream.LineTo(points[^1]);
            stream.LineTo(new Point(width, height));
            stream.EndFigure(true);
        }

        var fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(_primary, 0),
                new GradientStop(_secondary, 1)
            }
        };
        var lineColor = Mix(_primary, _secondary, 0.5);
        context.DrawGeometry(fill, new Pen(new SolidColorBrush(lineColor), 0.8), geometry);
    }

    private static Color Mix(Color left, Color right, double amount) => Color.FromRgb(
        (byte)Math.Round(left.R + (right.R - left.R) * amount),
        (byte)Math.Round(left.G + (right.G - left.G) * amount),
        (byte)Math.Round(left.B + (right.B - left.B) * amount));
}
