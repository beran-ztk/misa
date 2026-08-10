using System;
using Avalonia.Media;

namespace Music.Views;

/// <summary>
/// Shared semantic colors for analysis results.
/// </summary>
public static class AnalysisColorScale
{
    // MIREX scores rarely reach 1.0; .6 already represents a strong match.
    public static IBrush MoodModel(double score) => RedToGreen(Clamp(score / .6));

    public static IBrush GenreConfidence(double confidence) =>
        RedToGreen(Clamp(confidence / .5));

    public static IBrush Tempo(double bpm)
    {
        // Tempo is descriptive, not good or bad: cool for slow, warm for fast.
        var progress = Clamp((bpm - 60) / 150);
        return FromHsl(210 - (200 * progress), .76, .53);
    }

    public static IBrush IntegratedLoudness(double lufs)
    {
        // Around -11 LUFS is a balanced modern master. Both extremes move towards red.
        var distance = Math.Abs(lufs + 11);
        return RedToGreen(1 - Clamp(distance / 8));
    }

    public static IBrush LoudnessRange(double lu)
    {
        // Roughly 4–8 LU keeps useful contrast without excessive jumps.
        var distance = lu switch
        {
            < 4 => 4 - lu,
            > 8 => lu - 8,
            _ => 0
        };
        return RedToGreen(1 - Clamp(distance / 7));
    }

    private static IBrush RedToGreen(double progress) =>
        FromHsl(120 * Clamp(progress), .78, .5);

    private static IBrush FromHsl(double hue, double saturation, double lightness)
    {
        hue = ((hue % 360) + 360) % 360;
        var chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        var x = chroma * (1 - Math.Abs(((hue / 60) % 2) - 1));
        var match = lightness - (chroma / 2);
        var (red, green, blue) = hue switch
        {
            < 60 => (chroma, x, 0d),
            < 120 => (x, chroma, 0d),
            < 180 => (0d, chroma, x),
            < 240 => (0d, x, chroma),
            < 300 => (x, 0d, chroma),
            _ => (chroma, 0d, x)
        };
        return new SolidColorBrush(Color.FromRgb(
            (byte)Math.Round((red + match) * 255),
            (byte)Math.Round((green + match) * 255),
            (byte)Math.Round((blue + match) * 255)));
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);
}
