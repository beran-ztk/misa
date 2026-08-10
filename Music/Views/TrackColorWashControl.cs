using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Music.Views;

public sealed class TrackColorWashControl : Control
{
    public static readonly StyledProperty<Color> PrimaryColorProperty =
        AvaloniaProperty.Register<TrackColorWashControl, Color>(nameof(PrimaryColor), Colors.Transparent);

    public static readonly StyledProperty<Color> SecondaryColorProperty =
        AvaloniaProperty.Register<TrackColorWashControl, Color>(nameof(SecondaryColor), Colors.Transparent);

    public static readonly StyledProperty<int> SeedProperty =
        AvaloniaProperty.Register<TrackColorWashControl, int>(nameof(Seed));

    public static readonly StyledProperty<double> StrengthProperty =
        AvaloniaProperty.Register<TrackColorWashControl, double>(nameof(Strength), 0.2);

    public static readonly StyledProperty<double> ReachProperty =
        AvaloniaProperty.Register<TrackColorWashControl, double>(nameof(Reach), 60);

    private StreamGeometry? _cachedGeometry;
    private Size _cachedSize;
    private int _cachedSeed;
    private double _cachedReach;
    private Point _gradientStart;
    private Point _gradientEnd;

    static TrackColorWashControl()
    {
        AffectsRender<TrackColorWashControl>(
            PrimaryColorProperty,
            SecondaryColorProperty,
            SeedProperty,
            StrengthProperty,
            ReachProperty);
    }

    public Color PrimaryColor
    {
        get => GetValue(PrimaryColorProperty);
        set => SetValue(PrimaryColorProperty, value);
    }

    public Color SecondaryColor
    {
        get => GetValue(SecondaryColorProperty);
        set => SetValue(SecondaryColorProperty, value);
    }

    public int Seed
    {
        get => GetValue(SeedProperty);
        set => SetValue(SeedProperty, value);
    }

    public double Strength
    {
        get => GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    public double Reach
    {
        get => GetValue(ReachProperty);
        set => SetValue(ReachProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Bounds.Width <= 1 || Bounds.Height <= 1 || PrimaryColor.A == 0 || Strength <= 0)
            return;

        EnsureGeometry();
        if (_cachedGeometry is null)
            return;

        var opacity = Math.Clamp(Strength, 0, 0.6);
        DrawBaseWash(context, opacity);
        var primary = WithOpacity(PrimaryColor, opacity);
        var secondary = WithOpacity(SecondaryColor, opacity * 0.82);
        var softPrimary = WithOpacity(PrimaryColor, opacity * 0.28);
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(_gradientStart, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(_gradientEnd, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(WithOpacity(PrimaryColor, opacity * 0.62), 0),
                new GradientStop(primary, 0.14),
                new GradientStop(secondary, 0.56),
                new GradientStop(softPrimary, 0.86),
                new GradientStop(Colors.Transparent, 1)
            }
        };

        context.DrawGeometry(brush, null, _cachedGeometry);
    }

    private void DrawBaseWash(DrawingContext context, double opacity)
    {
        var endX = Bounds.Width * Math.Clamp(0.32 + Reach / 100d * 0.42, 0.40, 0.76);
        var brush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(new Point(0, Bounds.Height * 0.5), RelativeUnit.Absolute),
            EndPoint = new RelativePoint(new Point(endX, Bounds.Height * 0.5), RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(WithOpacity(PrimaryColor, opacity * 0.34), 0),
                new GradientStop(WithOpacity(SecondaryColor, opacity * 0.20), 0.38),
                new GradientStop(WithOpacity(PrimaryColor, opacity * 0.07), 0.76),
                new GradientStop(Colors.Transparent, 1)
            }
        };
        context.DrawRectangle(brush, null, new Rect(Bounds.Size));
    }

    private void EnsureGeometry()
    {
        var size = Bounds.Size;
        var reach = Math.Clamp(Reach, 20, 100);
        if (_cachedGeometry is not null
            && _cachedSize == size
            && _cachedSeed == Seed
            && Math.Abs(_cachedReach - reach) < 0.01)
            return;

        _cachedSize = size;
        _cachedSeed = Seed;
        _cachedReach = reach;
        _cachedGeometry = BuildOrganicGeometry(size, Seed, reach, out _gradientStart, out _gradientEnd);
    }

    private static StreamGeometry BuildOrganicGeometry(
        Size size,
        int seed,
        double reach,
        out Point gradientStart,
        out Point gradientEnd)
    {
        // The track seed is persisted with the track. Keeping it as the sole
        // source makes the generated shape stable across application starts.
        var random = new ShapeRandom(seed);
        var width = size.Width;
        var height = size.Height;
        var pattern = random.NextInt(6);
        var envelope = width * reach / 100d;
        var span = Math.Clamp(
            envelope * (0.88 + random.NextDouble() * 0.58),
            width * 0.42,
            width * 0.98);

        // Every wash enters through the left edge. Its identity comes from the
        // path and its lobes, not from detached islands elsewhere in the row.
        var startX = -height * (0.30 + random.NextDouble() * 0.12);
        var endX = Math.Min(width * 0.98, startX + span);
        var startY = height * (0.46 + random.NextDouble() * 0.08);
        double endY;

        switch (pattern)
        {
            case 0: // nearly horizontal flow
                endY = height * (0.44 + random.NextDouble() * 0.12);
                break;
            case 1: // gently rising flow
                endY = height * (0.39 + random.NextDouble() * 0.08);
                break;
            case 2: // gently falling flow
                endY = height * (0.53 + random.NextDouble() * 0.08);
                break;
            case 3: // centered S-flow
                endY = height * (0.45 + random.NextDouble() * 0.10);
                break;
            case 4: // shallow upper arc
                endY = height * (0.43 + random.NextDouble() * 0.09);
                break;
            default: // shallow lower arc
                endY = height * (0.48 + random.NextDouble() * 0.09);
                break;
        }

        const int pointCount = 10;
        var centers = new Point[pointCount];
        var radii = new double[pointCount];
        var bend = pattern switch
        {
            4 => -height * (0.10 + random.NextDouble() * 0.08),
            5 => height * (0.10 + random.NextDouble() * 0.08),
            _ => height * (random.NextDouble() - 0.5) * 0.22
        };
        var wave = height * (0.035 + random.NextDouble() * 0.055);
        var phase = random.NextDouble() * Math.PI * 2;
        var baseRadius = height * (0.29 + random.NextDouble() * 0.08);
        var firstLobe = 0.24 + random.NextDouble() * 0.22;
        var secondLobe = 0.58 + random.NextDouble() * 0.24;

        for (var index = 0; index < pointCount; index++)
        {
            var t = index / (double)(pointCount - 1);
            var envelopeT = Math.Sin(Math.PI * t);
            var x = Lerp(startX, endX, t);
            var y = Lerp(startY, endY, t)
                    + bend * envelopeT
                    + Math.Sin(t * Math.PI * 2 + phase) * wave * envelopeT;
            y = Math.Clamp(y, height * 0.39, height * 0.61);
            centers[index] = new Point(x, y);

            var lobeA = Math.Exp(-Math.Pow((t - firstLobe) / 0.19, 2)) * 0.25;
            var lobeB = Math.Exp(-Math.Pow((t - secondLobe) / 0.21, 2)) * 0.21;
            var breathing = 0.91 + Math.Sin(t * Math.PI * 2.4 + phase * 0.7) * 0.07;
            var desiredRadius = baseRadius * (breathing + lobeA + lobeB + envelopeT * 0.10);
            var availableRadius = Math.Max(height * 0.22, Math.Min(y - 3, height - y - 3));
            radii[index] = Math.Clamp(desiredRadius, height * 0.22, availableRadius);
        }

        // Enter broadly at the artwork and only allow gradual thickness changes.
        // This prevents one half of the wash from disappearing after a short run.
        radii[0] = Math.Min(height * 0.39, Math.Min(centers[0].Y - 3, height - centers[0].Y - 3));
        var maximumRadiusStep = height * 0.045;
        for (var index = 1; index < pointCount; index++)
        {
            var availableRadius = Math.Max(
                height * 0.22,
                Math.Min(centers[index].Y - 3, height - centers[index].Y - 3));
            var lowerBound = Math.Min(availableRadius, Math.Max(height * 0.22, radii[index - 1] - maximumRadiusStep));
            var upperBound = Math.Min(availableRadius, radii[index - 1] + maximumRadiusStep);
            radii[index] = Math.Clamp(radii[index], lowerBound, Math.Max(lowerBound, upperBound));
        }

        var top = new Point[pointCount];
        var bottom = new Point[pointCount];
        var tangents = new Vector[pointCount];
        var normals = new Vector[pointCount];
        for (var index = 0; index < pointCount; index++)
        {
            var previous = centers[Math.Max(0, index - 1)];
            var next = centers[Math.Min(pointCount - 1, index + 1)];
            var tangent = next - previous;
            var length = Math.Max(0.001, Math.Sqrt(tangent.X * tangent.X + tangent.Y * tangent.Y));
            tangents[index] = new Vector(tangent.X / length, tangent.Y / length);
            normals[index] = new Vector(-tangents[index].Y, tangents[index].X);
            top[index] = centers[index] + normals[index] * radii[index];
            bottom[index] = centers[index] - normals[index] * radii[index];
        }

        var outline = new List<Point>(pointCount * 2 + 8);
        outline.AddRange(top);

        // Rounded end cap: top -> outward tangent -> bottom.
        for (var step = 1; step <= 4; step++)
        {
            var angle = Math.PI * step / 4;
            var offset = normals[^1] * (Math.Cos(angle) * radii[^1])
                         + tangents[^1] * (Math.Sin(angle) * radii[^1]);
            outline.Add(centers[^1] + offset);
        }

        for (var index = pointCount - 2; index >= 0; index--)
            outline.Add(bottom[index]);

        // Rounded start cap: bottom -> reverse tangent -> top.
        for (var step = 1; step <= 3; step++)
        {
            var angle = Math.PI * step / 4;
            var offset = -normals[0] * (Math.Cos(angle) * radii[0])
                         - tangents[0] * (Math.Sin(angle) * radii[0]);
            outline.Add(centers[0] + offset);
        }

        gradientStart = centers[0];
        gradientEnd = centers[^1];
        return CreateSmoothClosedGeometry(outline);
    }

    private static StreamGeometry CreateSmoothClosedGeometry(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using var stream = geometry.Open();
        stream.BeginFigure(Midpoint(points[^1], points[0]), true);
        for (var index = 0; index < points.Count; index++)
        {
            var next = points[(index + 1) % points.Count];
            stream.QuadraticBezierTo(points[index], Midpoint(points[index], next));
        }
        stream.EndFigure(true);
        return geometry;
    }

    private static Point Midpoint(Point left, Point right) =>
        new((left.X + right.X) / 2, (left.Y + right.Y) / 2);

    private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;

    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Clamp((int)Math.Round(opacity * 255), 0, 255), color.R, color.G, color.B);

    private struct ShapeRandom
    {
        private uint _state;

        public ShapeRandom(int seed)
        {
            _state = unchecked((uint)seed * 747796405u + 2891336453u);
            if (_state == 0)
                _state = 0x9E3779B9u;
        }

        public double NextDouble()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (_state & 0x00FFFFFF) / 16777216d;
        }

        public int NextInt(int maximum) => Math.Min(maximum - 1, (int)(NextDouble() * maximum));
    }
}
