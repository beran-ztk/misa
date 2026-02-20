using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

namespace Misa.Contract.Items.Components.Schedules;

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

    public ItemDto Item { get; set; }
    
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

    public bool HasNoRestrictions => !HasSoftRestrictions && !HasHardRestrictions;
    // Soft restrictions (lifecycle/limits)
    public bool HasSoftRestrictions =>
        OccurrenceCountLimit is not null || ActiveUntilUtc is not null;

    // Hard restrictions (schedule constraints)
    public bool HasHardRestrictions =>
        ConstraintTimeWindow is not null ||
        ConstraintWeekdays is not null ||
        ConstraintMonths is not null ||
        ConstraintMonthDays is not null;

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

    private string SoftRestrictionClause
    {
        get
        {
            if (OccurrenceCountLimit is null && ActiveUntilUtc is null)
                return "with no limits";

            if (OccurrenceCountLimit is not null && ActiveUntilUtc is null)
                return $"up to {OccurrenceCountLimit} more time(s)";

            if (OccurrenceCountLimit is null && ActiveUntilUtc is not null)
                return ActiveUntilUtc >= UtcNow
                    ? $"until {FmtLocal(ActiveUntilUtc)}"
                    : $"ended at {FmtLocal(ActiveUntilUtc)}";

            return $"up to {OccurrenceCountLimit} more time(s), but no later than {FmtLocal(ActiveUntilUtc)}";
        }
    }
    public string TriggerSummary
    {
        get
        {
            var restriction = HasHardRestrictions ? " restricted" : string.Empty;
            
            var start = ActiveFromUtc <= UtcNow
                ? $"Runs{restriction} every {FrequencyString} since {FmtLocal(ActiveFromUtc)}"
                : $"Will run {restriction} every {FrequencyString}, starting at {FmtLocal(ActiveFromUtc)}";

            return $"{start} {SoftRestrictionClause}.";
        }
    }
    public string? ConstraintTimeWindow
    {
        get
        {
            if (StartTime is null && EndTime is null)
                return null;

            if (StartTime is not null && EndTime is null)
                return $"From {StartTime} onwards";

            if (StartTime is null && EndTime is not null)
                return $"Until {EndTime}";

            return $"Between {StartTime}–{EndTime}";
        }
    }

    public string? ConstraintWeekdays =>
        ByDay is { Length: > 0 }
            ? $"On {string.Join(", ", WeekdayNames(ByDay))}"
            : null;

    public string? ConstraintMonths =>
        ByMonth is { Length: > 0 }
            ? $"In {string.Join(", ", MonthNames(ByMonth))}"
            : null;

    public string? ConstraintMonthDays =>
        ByMonthDay is { Length: > 0 }
            ? $"On day {string.Join(", ", ByMonthDay)}"
            : null;

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
}
