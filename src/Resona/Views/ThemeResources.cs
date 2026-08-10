using System;
using Avalonia;
using Avalonia.Media;

namespace Resona.Views;

internal static class ThemeResources
{
    public static IBrush Brush(string key)
    {
        if (Application.Current?.TryGetResource(key, null, out var value) == true
            && value is IBrush brush)
            return brush;

        throw new InvalidOperationException($"Theme brush '{key}' is not defined.");
    }
}
