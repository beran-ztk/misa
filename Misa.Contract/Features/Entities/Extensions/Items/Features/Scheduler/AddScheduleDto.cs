namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed class AddScheduleDto
{
    public string Title { get; init; } = string.Empty;
    public required ScheduleFrequencyTypeDto ScheduleFrequencyType { get; init; }
    public required int FrequencyInterval { get; init; }

    public int LookaheadCount { get; init; }
    public int? OccurrenceCountLimit { get; init; }
    public ScheduleMisfirePolicyDto MisfirePolicy { get; init; } = ScheduleMisfirePolicyDto.Catchup;

    public TimeSpan? OccurrenceTtl { get; init; }
    
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public DateTimeOffset ActiveFromUtc { get; init; }
    public DateTimeOffset? ActiveUntilUtc { get; init; }
}