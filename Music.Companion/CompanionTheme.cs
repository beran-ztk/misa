using Avalonia;
using Avalonia.Media;

namespace Music.Companion;

internal static class CompanionTheme
{
    public static IBrush Brush(string key)
    {
        if (Application.Current?.TryGetResource(key, null, out var value) == true
            && value is IBrush brush)
            return brush;

        throw new InvalidOperationException($"Mobile theme brush '{key}' is not defined.");
    }
}
