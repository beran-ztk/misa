using Misa.Domain.Common.DomainEvents;
using Misa.Domain.Items.Components.Audits.Changes;

namespace Misa.Domain.Items.Components.Schedules;

/// <summary>
/// Represents a scheduled task configuration.
/// </summary>
public sealed class Schedule : DomainEventEntity
{
    private Schedule() { } // EF Core
    
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
    public void ChangeMisfirePolicy(ScheduleMisfirePolicy policy)
    {
        if (MisfirePolicy == policy) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.MisfirePolicy, MisfirePolicy.ToString(), policy.ToString(), null));
        MisfirePolicy = policy;
    }

    public void ChangeLookaheadLimit(int limit)
    {
        if (LookaheadLimit == limit) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.LookaheadLimit, LookaheadLimit.ToString(), limit.ToString(), null));
        LookaheadLimit = limit;
    }

    public void ChangeOccurrenceCountLimit(int? limit)
    {
        if (OccurrenceCountLimit == limit) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.OccurrenceCountLimit, OccurrenceCountLimit?.ToString(), limit?.ToString(), null));
        OccurrenceCountLimit = limit;
    }

    public void ChangeStartTime(TimeOnly? startTime)
    {
        if (StartTime == startTime) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.StartTime, StartTime?.ToString(), startTime?.ToString(), null));
        StartTime = startTime;
    }

    public void ChangeEndTime(TimeOnly? endTime)
    {
        if (EndTime == endTime) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.EndTime, EndTime?.ToString(), endTime?.ToString(), null));
        EndTime = endTime;
    }

    public void ChangeActiveUntil(DateTimeOffset? activeUntilUtc)
    {
        if (ActiveUntilUtc == activeUntilUtc) return;
        AddDomainEvent(new PropertyChangedEvent(Id.Value, ChangeType.ActiveUntil, ActiveUntilUtc?.ToString("O"), activeUntilUtc?.ToString("O"), null));
        ActiveUntilUtc = activeUntilUtc;
    }
    public void CheckAndUpdateNextAllowedExecution()
    {
        var utcNow = DateTimeOffset.UtcNow;
        NextAllowedExecutionAtUtc = NextAllowedExecutionAtUtc >= utcNow
            ? NextAllowedExecutionAtUtc
            : SchedulingAnchorUtc;
    }
    public void ReduceOccurrenceCount()
    {
        if (OccurrenceCountLimit is > 0)
        {
            OccurrenceCountLimit -= 1;
        }
    }
    
    // Constructors
    public ScheduleExecutionLog CreateExecutionLog()
    {
        return new ScheduleExecutionLog(Id, SchedulingAnchorUtc);
    }
    public Schedule(
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
        int[]? byMonth)
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
