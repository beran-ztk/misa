using System;
using Avalonia;
using Avalonia.Media;
using Music.Models;

namespace Music.Views;

internal sealed record InterfaceThemePalette(
    Color TintLayer,
    Color LibraryLayer,
    Color PlayerDarkening);

internal static class InterfaceThemeService
{
    private static readonly Color NeutralTint = Color.Parse("#505050");

    public static InterfaceThemePalette Apply(AppearanceSettings settings)
    {
        var amount = settings.InterfaceTintEnabled
            ? settings.InterfaceTintStrength / 100d
            : 0;
        var selected = Parse(settings.InterfaceTintColor, Color.Parse("#6E6748"));
        var tint = Mix(NeutralTint, selected, amount);

        SetBrush("Theme.Brush.Background", Shade(tint, 0.25));
        SetBrush("Theme.Brush.Overlay", WithAlpha(Shade(tint, 0.25), 232));
        SetBrush("Theme.Brush.Navigation", WithAlpha(Shade(tint, 0.30), 176));
        SetBrush("Theme.Brush.Surface", Shade(tint, 0.30));
        SetBrush("Theme.Brush.SurfaceRaised", Shade(tint, 0.38));
        SetBrush("Theme.Brush.SurfaceTranslucent", WithAlpha(Shade(tint, 0.38), 168));
        SetBrush("Theme.Brush.SurfaceHover", Shade(tint, 0.52));
        SetBrush("Theme.Brush.SurfaceSelected", Shade(tint, 0.68));
        SetBrush("Theme.Brush.AccentSurface", Shade(tint, 0.72));
        SetBrush("Theme.Brush.Input", WithAlpha(Shade(tint, 0.21), 122));

        SetBrush("Theme.Brush.BorderSubtle", Shade(tint, 0.67));
        SetBrush("Theme.Brush.Border", Shade(tint, 0.76));
        SetBrush("Theme.Brush.BorderStrong", Shade(tint, 0.87));
        SetBrush("Theme.Brush.Divider", Shade(tint, 0.64));

        var accent = Lighten(tint, 1.65);
        SetBrush("Theme.Brush.Accent", accent);
        SetBrush("Theme.Brush.AccentStrong", Lighten(tint, 1.85));
        SetBrush("Theme.Brush.TextSecondary", Mix(Color.Parse("#D5D5CF"), accent, 0.22));
        SetBrush("Theme.Brush.TextMuted", Mix(Color.Parse("#C2C2BC"), tint, 0.10));

        var tintAlpha = settings.InterfaceTintEnabled
            ? (byte)Math.Round(92 * amount)
            : (byte)0;
        return new InterfaceThemePalette(
            WithAlpha(selected, tintAlpha),
            WithAlpha(selected, (byte)Math.Round(tintAlpha * 0.65)),
            settings.InterfaceTintEnabled ? Shade(tint, 0.36) : Color.Parse("#242424"));
    }

    private static void SetBrush(string key, Color color)
    {
        if (Application.Current?.TryGetResource(key, null, out var value) == true
            && value is SolidColorBrush brush)
        {
            brush.Color = color;
        }
    }

    private static Color Shade(Color color, double factor) => Color.FromRgb(
        ToByte(color.R * factor),
        ToByte(color.G * factor),
        ToByte(color.B * factor));

    private static Color Lighten(Color color, double factor) => Color.FromRgb(
        ToByte(color.R * factor),
        ToByte(color.G * factor),
        ToByte(color.B * factor));

    private static Color WithAlpha(Color color, byte alpha) =>
        Color.FromArgb(alpha, color.R, color.G, color.B);

    private static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            ToByte(from.R + (to.R - from.R) * amount),
            ToByte(from.G + (to.G - from.G) * amount),
            ToByte(from.B + (to.B - from.B) * amount));
    }

    private static Color Parse(string value, Color fallback)
    {
        try { return Color.Parse(value); }
        catch { return fallback; }
    }

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value), 0, 255);
}
