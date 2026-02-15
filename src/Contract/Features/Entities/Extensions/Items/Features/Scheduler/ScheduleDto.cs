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
    public string Title => Item.Title;
    public string ScheduleActivity =>
        ActiveFromUtc <= UtcNow
            ? $"Triggers every {FrequencyInterval} {FrequencyType} since {ActiveFromUtc}"
            : $"Will be triggering every {FrequencyInterval} {FrequencyType} upon reaching {ActiveFromUtc}";
    public string ComingActivity
    {
        get
        {
            if (LastRunAtUtc is null && NextDueAtUtc is null)
                return "Has not triggered yet and no future execution is scheduled.";

            if (LastRunAtUtc is not null && NextDueAtUtc is null)
                return $"Last triggered at {LastRunAtUtc}, with no future execution scheduled.";

            if (LastRunAtUtc is null && NextDueAtUtc is not null)
                return $"Has not triggered yet. First execution scheduled for {NextDueAtUtc}.";

            return $"Last triggered at {LastRunAtUtc}. Next execution scheduled for {NextDueAtUtc}.";
        }
    }
    public string ActivityBehaviour
    {
        get
        {
            if (OccurrenceCountLimit is null && ActiveUntilUtc is null)
                return $"Will run indefinitely.";

            if (OccurrenceCountLimit is not null && ActiveUntilUtc is null)
                return $"Will trigger {OccurrenceCountLimit} more times.";

            if (OccurrenceCountLimit is null && ActiveUntilUtc is not null)
                return ActiveUntilUtc >= UtcNow
                ? $"Will be active until {ActiveUntilUtc}."
                : $"Has reached user-set time-limit at {ActiveUntilUtc}";   

            return $"Will trigger {OccurrenceCountLimit} more times, but only until {ActiveUntilUtc}.";
        }
    }
    public string OperationalBehaviour
    {
        get
        {
            var parts = new List<string>();

            if (LookaheadLimit > 1)
                parts.Add($"Plans {LookaheadLimit} occurrences ahead.");

            if (OccurrenceTtl is not null)
                parts.Add($"Occurrences expire after {OccurrenceTtl}.");

            parts.Add($"In case of missed triggers: {MisfirePolicy}.");

            return string.Join(" ", parts);
        }
    }
    public string RestrictionsBehaviour
    {
        get
        {
            var parts = new List<string>();

            // Time window
            if (StartTime is not null && EndTime is null)
                parts.Add($"Executes from {StartTime} onwards.");

            else if (StartTime is null && EndTime is not null)
                parts.Add($"Executes until {EndTime}.");

            else if (StartTime is not null && EndTime is not null)
                parts.Add($"Executes between {StartTime} and {EndTime}.");

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

                parts.Add($"Only on {string.Join(", ", weekdayNames)}.");
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

                parts.Add($"Only in {string.Join(", ", monthNames)}.");
            }

            // Month days (1–31)
            if (ByMonthDay is not null && ByMonthDay.Length != 0)
            {
                parts.Add($"Only on day {string.Join(", ", ByMonthDay)} of the month.");
            }

            return string.Join(" ", parts);
        }
    }
    public string? ExecutionAvailability =>
        NextAllowedExecutionAtUtc is null
            ? null
            : $"Execution currently blocked until {NextAllowedExecutionAtUtc}.";
}
