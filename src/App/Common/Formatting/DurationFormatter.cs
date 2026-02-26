using System;

namespace Misa.Ui.Avalonia.Common.Formatting;

public static class DurationFormatter
{
    public static string FormatDuration(TimeSpan? duration)
    {
        return duration switch
        {
            null => string.Empty,

            { TotalDays: >= 1 } ts
                => $"{(int)ts.TotalDays}:{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00} d",

            { TotalHours: >= 1 and < 24 } ts
                => $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00} h",

            { TotalMinutes: >= 0 and < 60 } ts
                => $"{(int)ts.TotalMinutes}:{ts.Seconds:00} min",

            _ => string.Empty
        };
    }
}