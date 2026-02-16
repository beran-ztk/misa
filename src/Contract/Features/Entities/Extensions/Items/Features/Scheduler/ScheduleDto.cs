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
                return value.EndsWith("s") ? value[..^1] : value;

            return $"{FrequencyInterval} {value}";
        }
    }

    // Soft restrictions (lifecycle/limits)
    public bool HasSoftRestrictions =>
        OccurrenceCountLimit is not null || ActiveUntilUtc is not null;

    // Hard restrictions (schedule constraints)
    public bool HasHardRestrictions =>
        StartTime is not null ||
        EndTime is not null ||
        (ByDay is not null && ByDay.Length != 0) ||
        (ByMonth is not null && ByMonth.Length != 0) ||
        (ByMonthDay is not null && ByMonthDay.Length != 0);

    public bool HasAnyRestrictions => HasSoftRestrictions || HasHardRestrictions;

    private string SoftRestrictionClause
    {
        get
        {
            // Keep this compact; it’s meant to be merged into the main line.
            if (OccurrenceCountLimit is null && ActiveUntilUtc is null)
                return "no limits";

            if (OccurrenceCountLimit is not null && ActiveUntilUtc is null)
                return $"up to {OccurrenceCountLimit} more time(s)";

            if (OccurrenceCountLimit is null && ActiveUntilUtc is not null)
                return ActiveUntilUtc >= UtcNow
                    ? $"until {FmtLocal(ActiveUntilUtc)}"
                    : $"ended at {FmtLocal(ActiveUntilUtc)}";

            return $"up to {OccurrenceCountLimit} more time(s), but no later than {FmtLocal(ActiveUntilUtc)}";
        }
    }

    private static IEnumerable<string> WeekdayNames(int[] days) =>
        days.Select(d => d switch
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
            .Where(n => n is not null)!;

    private static IEnumerable<string> MonthNames(int[] months) =>
        months.Select(m => m switch
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
            .Where(n => n is not null)!;

    public string TriggerSummary
    {
        get
        {
            var start = ActiveFromUtc <= UtcNow
                ? $"Runs every {FrequencyString} since {FmtLocal(ActiveFromUtc)}"
                : $"Will run every {FrequencyString}, starting at {FmtLocal(ActiveFromUtc)}";

            // Merge “ActivityBehaviour” into the first line (soft restrictions).
            // If it’s truly unlimited, keep it short.
            var limits = SoftRestrictionClause == "no limits"
                ? "with no limits"
                : $"({SoftRestrictionClause})";

            // Optional hint about hard restrictions without duplicating the full text here.
            var hardHint = HasHardRestrictions ? "Hard restrictions apply." : string.Empty;

            return $"{start} {limits}. {hardHint}";
        }
    }

    public string ScheduleConstraints
    {
        get
        {
            if (!HasHardRestrictions)
                return "No schedule constraints.";

            var parts = new List<string>();

            // Time window
            if (StartTime is not null && EndTime is null)
                parts.Add($"From {StartTime} onwards");
            else if (StartTime is null && EndTime is not null)
                parts.Add($"Until {EndTime}");
            else if (StartTime is not null && EndTime is not null)
                parts.Add($"Between {StartTime}–{EndTime}");

            // Days
            if (ByDay is not null && ByDay.Length != 0)
                parts.Add($"On {string.Join(", ", WeekdayNames(ByDay))}");

            // Months
            if (ByMonth is not null && ByMonth.Length != 0)
                parts.Add($"In {string.Join(", ", MonthNames(ByMonth))}");

            // Month days
            if (ByMonthDay is not null && ByMonthDay.Length != 0)
                parts.Add($"On day {string.Join(", ", ByMonthDay)}");

            return string.Join(". ", parts) + ".";
        }
    }

    public string ExecutionStatus
    {
        get
        {
            if (LastRunAtUtc is null && NextDueAtUtc is null)
                return "No executions yet; none scheduled.";

            if (LastRunAtUtc is not null && NextDueAtUtc is null)
                return $"Last: {FmtLocal(LastRunAtUtc)}. Next: none scheduled.";

            if (LastRunAtUtc is null && NextDueAtUtc is not null)
                return $"First scheduled: {FmtLocal(NextDueAtUtc)}.";

            return $"Last: {FmtLocal(LastRunAtUtc)}. Next: {FmtLocal(NextDueAtUtc)}.";
        }
    }

    public string OperationalBehaviour
    {
        get
        {
            var parts = new List<string>();

            if (LookaheadLimit > 1)
                parts.Add($"Precomputes {LookaheadLimit} occurrence(s)");

            if (OccurrenceTtl is not null)
                parts.Add($"TTL {OccurrenceTtl}");

            parts.Add($"Misfire {MisfirePolicy}");

            return string.Join(". ", parts) + ".";
        }
    }

    public string? ExecutionAvailability =>
        NextAllowedExecutionAtUtc is null
            ? null
            : $"Blocked until {FmtLocal(NextAllowedExecutionAtUtc)}.";

}
