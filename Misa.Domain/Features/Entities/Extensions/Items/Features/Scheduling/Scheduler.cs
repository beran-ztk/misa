using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

/// <summary>
/// Represents a scheduled task configuration.
/// </summary>
public sealed class Scheduler
{
    private Scheduler() { } // EF Core
    
    private Scheduler(Item item)
    {
        Item = item;
    }

    public static Scheduler Create(string title, ScheduleFrequencyType frequencyType, int frequencyInterval)
    {
        if (frequencyInterval <= 0)
            throw new ArgumentException("Frequency interval must be greater than 0", nameof(frequencyInterval));
        
        var item = Item.Create(Workflow.Scheduling, title, Priority.None);
        
        return new Scheduler(item)
        {
            ScheduleFrequencyType = frequencyType,
            FrequencyInterval = frequencyInterval,
            MisfirePolicy = ScheduleMisfirePolicy.Catchup,
            LookaheadCount = 1,
            ActiveFromUtc = DateTimeOffset.UtcNow,
            Timezone = "utc"
        };
    }

    public Guid Id { get; private set; }
    
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
    
    public Item Item { get; private set; } = null!;
    public ICollection<SchedulerExecutionLog> ExecutionLogs { get; private set; } = new List<SchedulerExecutionLog>();
}
