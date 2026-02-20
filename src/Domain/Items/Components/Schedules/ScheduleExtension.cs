using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Domain.Items.Components.Schedules;

/// <summary>
/// Represents a scheduled task configuration.
/// </summary>
public sealed class ScheduleExtension
{
    private ScheduleExtension() { } // EF Core
    
    // Fields + Properties
    public ItemId Id { get; init; }
    public Guid? TargetItemId { get; private set; }
    
    public ScheduleFrequencyType ScheduleFrequencyType { get; private set; }
    public int FrequencyInterval { get; private set; }

    public int? OccurrenceCountLimit { get; private set; }
    
    public int[]? ByDay { get; private set; }
    public int[]? ByMonthDay { get; private set; }
    public int[]? ByMonth { get; private set; }
    
    public ScheduleMisfirePolicy MisfirePolicy { get; private set; }
    
    public int LookaheadLimit { get; private set; }
    
    public TimeSpan? OccurrenceTtl { get; private set; }
    
    public ScheduleActionType ActionType { get; private set; }
    public string? Payload { get; private set; }
    
    public string Timezone { get; private set; } = string.Empty;
    
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    
    public DateTimeOffset ActiveFromUtc { get; init; }
    public DateTimeOffset? ActiveUntilUtc { get; private set; }
    
    public DateTimeOffset? LastRunAtUtc { get; private set; }
    public DateTimeOffset? NextDueAtUtc { get; set; }
    public DateTimeOffset? NextAllowedExecutionAtUtc { get; set; }

    // Derived Properties
    public DateTimeOffset SchedulingAnchorUtc =>
        NextDueAtUtc ?? ActiveFromUtc;
    
    // Components
    public ICollection<ScheduleExecutionLog> ExecutionLogs { get; private set; } = new List<ScheduleExecutionLog>();

    // Mutators
    public void CheckAndUpdateNextAllowedExecution(DateTimeOffset utcNow)
        => NextAllowedExecutionAtUtc = NextAllowedExecutionAtUtc >= utcNow 
            ? NextAllowedExecutionAtUtc 
            : SchedulingAnchorUtc;
    public void ReduceOccurrenceCount()
    {
        if (OccurrenceCountLimit is > 0)
        {
            OccurrenceCountLimit -= 1;
        }
    }
    
    // Constructors
    public ScheduleExecutionLog CreateExecutionLog(Guid executionLogId, DateTimeOffset utcNow)
    {
        return new ScheduleExecutionLog(executionLogId, Id.Value, SchedulingAnchorUtc, utcNow);
    }
    public ScheduleExtension(
        ItemId id,
        Guid? targetItemId,
        ScheduleFrequencyType frequencyType,
        int frequencyInterval,
        int lookaheadLimit,
        int? occurrenceCountLimit,
        ScheduleMisfirePolicy misfirePolicy,
        TimeSpan? occurrenceTtl,
        ScheduleActionType actionType,
        string? payload,
        TimeOnly? startTime,
        TimeOnly? endTime,
        DateTimeOffset activeFromUtc,
        DateTimeOffset? activeUntilUtc,
        int[]? byDay,
        int[]? byMonthDay,
        int[]? byMonth,
        string timezone)
    {
        if (frequencyInterval <= 0)
            throw new ArgumentException("Frequency interval must be greater than 0.", nameof(frequencyInterval));

        if (lookaheadLimit <= 0)
            throw new ArgumentException("LookaheadCount must be greater than 0.", nameof(lookaheadLimit));

        if (occurrenceCountLimit is <= 0)
            throw new ArgumentException("OccurrenceCountLimit must be greater than 0 when provided.", nameof(occurrenceCountLimit));

        if (occurrenceTtl is { } ttl && ttl <= TimeSpan.Zero)
            throw new ArgumentException("OccurrenceTtl must be greater than 0 when provided.", nameof(occurrenceTtl));

        if (activeUntilUtc is { } until && until <= activeFromUtc)
            throw new ArgumentException("ActiveUntilUtc must be greater than ActiveFromUtc when provided.", nameof(activeUntilUtc));

        if (startTime is { } st && endTime is { } et && et <= st)
            throw new ArgumentException("EndTime must be greater than StartTime when both are provided.", nameof(endTime));

        var normalizedByDay = Normalize(byDay, 0, 6, nameof(byDay));
        var normalizedByMonthDay = Normalize(byMonthDay, 1, 31, nameof(byMonthDay));
        var normalizedByMonth = Normalize(byMonth, 1, 12, nameof(byMonth));


        Id = id;
        TargetItemId = targetItemId;
        ScheduleFrequencyType = frequencyType;
        FrequencyInterval = frequencyInterval;

        OccurrenceCountLimit = occurrenceCountLimit;
        MisfirePolicy = misfirePolicy;
        OccurrenceTtl = occurrenceTtl;
        ActionType = actionType;
        Payload = payload;

        StartTime = startTime;
        EndTime = endTime;

        ActiveFromUtc = activeFromUtc;
        ActiveUntilUtc = activeUntilUtc;

        LookaheadLimit = lookaheadLimit;
        Timezone = timezone;

        ByDay = normalizedByDay;
        ByMonthDay = normalizedByMonthDay;
        ByMonth = normalizedByMonth;

        static int[]? Normalize(int[]? values, int min, int max, string name)
        {
            if (values is null || values.Length == 0) return null;

            var distinct = values.Distinct().ToArray();
            if (distinct.Any(v => v < min || v > max))
                throw new ArgumentException($"{name} contains invalid values.", name);

            Array.Sort(distinct);
            return distinct;
        }
    }
}
