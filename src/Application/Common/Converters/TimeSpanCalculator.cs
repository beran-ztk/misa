namespace Misa.Core.Common.Converters;
public record StartToEndTimestamp(DateTimeOffset StartedAtUtc, DateTimeOffset? EndedAtUtc);
public static class TimeSpanCalculator
{
    public static TimeSpan? ElapsedTime(List<StartToEndTimestamp> timestamps)
    {
        return timestamps.Aggregate(TimeSpan.Zero, (sum, s) =>
        {
            var end = s.EndedAtUtc ?? DateTimeOffset.UtcNow;
            return sum + (end - s.StartedAtUtc);
        });
    }
    
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