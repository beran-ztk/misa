using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Resona.Services;

public static class WindowPlacementStore
{
    private const int MinimumVisiblePixels = 120;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static WindowPlacement? Apply(Window window)
    {
        var placement = Load();
        if (placement is null)
            return null;

        var normal = placement.NormalBounds;
        if (!IsUsable(normal, window))
            return null;

        var visibleBounds = EnsureVisible(normal, window.Screens.All);
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Position = new PixelPoint(visibleBounds.X, visibleBounds.Y);
        window.Width = visibleBounds.Width;
        window.Height = visibleBounds.Height;

        if (placement.WindowState is WindowState.Maximized or WindowState.FullScreen)
            window.WindowState = placement.WindowState.Value;

        return placement with { NormalBounds = visibleBounds };
    }

    public static void Save(Window window, WindowBounds? normalBounds = null)
    {
        try
        {
            var bounds = normalBounds ?? WindowBounds.FromWindow(window);
            if (!IsUsable(bounds, window))
                return;

            var screen = window.Screens.ScreenFromBounds(bounds.ToPixelRect());
            var placement = new WindowPlacement(
                bounds,
                window.WindowState,
                screen?.DisplayName);

            Directory.CreateDirectory(Values.LocalDirectory);
            var json = JsonSerializer.Serialize(placement, JsonOptions);
            File.WriteAllText(Values.WindowPlacementPath, json);
        }
        catch
        {
            // Window placement is convenience state; never block app shutdown/startup on it.
        }
    }

    private static WindowPlacement? Load()
    {
        try
        {
            if (!File.Exists(Values.WindowPlacementPath))
                return null;

            var json = File.ReadAllText(Values.WindowPlacementPath);
            return JsonSerializer.Deserialize<WindowPlacement>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static bool IsUsable(WindowBounds bounds, Window window)
    {
        var minWidth = Math.Max(1, (int)Math.Ceiling(window.MinWidth));
        var minHeight = Math.Max(1, (int)Math.Ceiling(window.MinHeight));
        return bounds.Width >= minWidth
               && bounds.Height >= minHeight
               && bounds.Width <= 10000
               && bounds.Height <= 10000;
    }

    private static WindowBounds EnsureVisible(WindowBounds bounds, IReadOnlyList<Screen> screens)
    {
        if (screens.Count == 0)
            return bounds;

        if (screens.Any(screen => VisibleEnough(bounds, screen.WorkingArea)))
            return bounds;

        var target = screens.FirstOrDefault(screen => screen.IsPrimary) ?? screens[0];
        var area = target.WorkingArea;
        var width = Math.Min(bounds.Width, area.Width);
        var height = Math.Min(bounds.Height, area.Height);
        return bounds with
        {
            X = area.X + Math.Max(0, (area.Width - width) / 2),
            Y = area.Y + Math.Max(0, (area.Height - height) / 2),
            Width = width,
            Height = height
        };
    }

    private static bool VisibleEnough(WindowBounds bounds, PixelRect area)
    {
        var left = Math.Max(bounds.X, area.X);
        var top = Math.Max(bounds.Y, area.Y);
        var right = Math.Min(bounds.X + bounds.Width, area.X + area.Width);
        var bottom = Math.Min(bounds.Y + bounds.Height, area.Y + area.Height);
        return right - left >= MinimumVisiblePixels
               && bottom - top >= MinimumVisiblePixels;
    }
}

public sealed record WindowPlacement(
    WindowBounds NormalBounds,
    WindowState? WindowState,
    string? ScreenDisplayName);

public sealed record WindowBounds(int X, int Y, int Width, int Height)
{
    public static WindowBounds FromWindow(Window window)
    {
        var size = window.ClientSize.Width > 0 && window.ClientSize.Height > 0
            ? window.ClientSize
            : new Size(
            Math.Max(1, window.Width),
            Math.Max(1, window.Height));
        return new WindowBounds(
            window.Position.X,
            window.Position.Y,
            Math.Max(1, (int)Math.Round(size.Width)),
            Math.Max(1, (int)Math.Round(size.Height)));
    }

    public PixelRect ToPixelRect() => new(new PixelPoint(X, Y), new PixelSize(Width, Height));
}
