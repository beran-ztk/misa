using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents a scheduled task configuration.
/// </summary>
public sealed class Scheduler
{
    private Scheduler() { } // EF Core

    public static Scheduler CreateAndInitDefaultValues(Item item)
    {
        var scheduler = new Scheduler
        {
            ItemId = item.EntityId,
            ScheduleFrequencyType = ScheduleFrequencyType.Minutes,
            FrequencyInterval = 1,
            MisfirePolicy = ScheduleMisfirePolicy.Catchup,
            LookaheadCount = 1,
            ActiveFromUtc = DateTimeOffset.UtcNow,
            Timezone = "utc"
        };
        
        return scheduler;
    }

    public Guid Id { get; private set; }
    public Guid ItemId { get; private set; }
    
    public ScheduleFrequencyType ScheduleFrequencyType { get; private set; }
    public int FrequencyInterval { get; private set; }

    public int? OccurrenceCountLimit { get; private set; }
    
    public int[]? ByDay { get; private set; }
    public int[]? ByMonthDay { get; private set; }
    public int[]? ByMonth { get; private set; }
    
    public ScheduleMisfirePolicy MisfirePolicy { get; private set; }
    
    public int LookaheadCount { get; private set; }
    
    public TimeSpan? OccurrenceTtl { get; private set; }
    
    public string? Payload { get; private set; }
    
    public string Timezone { get; private set; } = string.Empty;
    
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    
    public DateTimeOffset ActiveFromUtc { get; private set; }
    public DateTimeOffset? ActiveUntilUtc { get; private set; }
    
    public DateTimeOffset? LastRunAtUtc { get; private set; }
    public DateTimeOffset? NextDueAtUtc { get; private set; }
    
    public ICollection<SchedulerExecutionLog> ExecutionLogs { get; private set; } = new List<SchedulerExecutionLog>();
}
