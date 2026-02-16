using System.Runtime.Serialization;
using Misa.Contract.Features.Entities.Extensions.Items.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed class ScheduleDto
{
    public Guid Id { get; init; }

    public ScheduleFrequencyTypeDto FrequencyType { get; init; }
    public int FrequencyInterval { get; init; }

    public int? OccurrenceCountLimit { get; init; }

    public int[]? ByDay { get; init; }
    public int[]? ByMonthDay { get; init; }
    public int[]? ByMonth { get; init; }

    public ScheduleMisfirePolicyDto MisfirePolicy { get; init; }

    public int LookaheadLimit { get; init; }
    public TimeSpan? OccurrenceTtl { get; init; }

    public string? Payload { get; init; }
    public string Timezone { get; init; } = string.Empty;

    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public DateTimeOffset ActiveFromUtc { get; init; }
    public DateTimeOffset? ActiveUntilUtc { get; init; }

    public DateTimeOffset? LastRunAtUtc { get; init; }
    public DateTimeOffset? NextDueAtUtc { get; init; }
    public DateTimeOffset? NextAllowedExecutionAtUtc { get; init; }

    public required ItemDto Item { get; init; }
    
    // Derived Summary
    private DateTimeOffset UtcNow { get; } = DateTimeOffset.UtcNow;
    private const string DateTimeFormat = "dd/MM/yyyy HH:mm:ss";
    private static string FmtLocal(DateTimeOffset utc)
        => utc.ToLocalTime().ToString(DateTimeFormat);
    private static string? FmtLocal(DateTimeOffset? utc)
        => utc is null ? null : FmtLocal(utc.Value);
    private string FrequencyString
    {
        get
        {
            var value = FrequencyType.ToString();

            if (FrequencyInterval == 1)
                return value.EndsWith("s")
                    ? value[..^1]
                    : value;

            return $"{FrequencyInterval} {value}";
        }
    }
    
    public string TriggerFrequency =>
        ActiveFromUtc <= UtcNow
            ? $"Runs every {FrequencyString} since {FmtLocal(ActiveFromUtc)}."
            : $"Will run every {FrequencyString}, starting at {FmtLocal(ActiveFromUtc)}.";
    
    public string ComingActivity
    {
        get
        {
            if (LastRunAtUtc is null && NextDueAtUtc is null)
                return "No executions have occurred and none are currently scheduled.";

            if (LastRunAtUtc is not null && NextDueAtUtc is null)
                return $"Last execution was at {FmtLocal(LastRunAtUtc)}. No further executions are scheduled.";

            if (LastRunAtUtc is null && NextDueAtUtc is not null)
                return $"No executions have occurred yet. First execution is scheduled for {FmtLocal(NextDueAtUtc)}.";

            return $"Last execution was at {FmtLocal(LastRunAtUtc)}. Next execution is scheduled for {FmtLocal(NextDueAtUtc)}.";
        }
    }
    public string ActivityBehaviour
    {
        get
        {
            if (OccurrenceCountLimit is null && ActiveUntilUtc is null)
                return "Runs indefinitely.";

            if (OccurrenceCountLimit is not null && ActiveUntilUtc is null)
                return $"Will run {OccurrenceCountLimit} more times.";

            if (OccurrenceCountLimit is null && ActiveUntilUtc is not null)
                return ActiveUntilUtc >= UtcNow
                    ? $"Active until {FmtLocal(ActiveUntilUtc)}."
                    : $"Ended at {FmtLocal(ActiveUntilUtc)} due to the configured time limit.";

            return $"Could run {OccurrenceCountLimit} more times, but no later than {FmtLocal(ActiveUntilUtc)}.";
        }
    }

    public string OperationalBehaviour
    {
        get
        {
            var parts = new List<string>();

            if (LookaheadLimit > 1)
                parts.Add($"Precomputes {LookaheadLimit} future occurrences.");

            if (OccurrenceTtl is not null)
                parts.Add($"Occurrences expire after {OccurrenceTtl}.");

            parts.Add($"Misfire policy: {MisfirePolicy}.");

            return string.Join(" ", parts);
        }
    }
    public string RestrictionsBehaviour
    {
        get
        {
            var parts = new List<string>();

            if (StartTime is not null && EndTime is null)
                parts.Add($"Daily from {StartTime} onwards.");
            else if (StartTime is null && EndTime is not null)
                parts.Add($"Daily until {EndTime}.");
            else if (StartTime is not null && EndTime is not null)
                parts.Add($"Daily between {StartTime} and {EndTime}.");

            // Weekdays (0 = Sunday, 1 = Monday, ...)
            if (ByDay is not null && ByDay.Length != 0)
            {
                var weekdayNames = ByDay
                    .Select(d => d switch
                    {
                        0 => "Sunday",
                        1 => "Monday",
                        2 => "Tuesday",
                        3 => "Wednesday",
                        4 => "Thursday",
                        5 => "Friday",
                        6 => "Saturday",
                        _ => null
                    })
                    .Where(n => n is not null);

                parts.Add($"On {string.Join(", ", weekdayNames)}.");
            }

            // Months (1–12)
            if (ByMonth is not null && ByMonth.Length != 0)
            {
                var monthNames = ByMonth
                    .Select(m => m switch
                    {
                        1 => "January",
                        2 => "February",
                        3 => "March",
                        4 => "April",
                        5 => "May",
                        6 => "June",
                        7 => "July",
                        8 => "August",
                        9 => "September",
                        10 => "October",
                        11 => "November",
                        12 => "December",
                        _ => null
                    })
                    .Where(n => n is not null);

                parts.Add($"On {string.Join(", ", monthNames)}.");
            }

            // Month days (1–31)
            if (ByMonthDay is not null && ByMonthDay.Length != 0)
                parts.Add($"Only on day {string.Join(", ", ByMonthDay)} of the month.");

            return string.Join("\n", parts);
        }
    }
}
