namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed class AddScheduleDto
{
    public required string Title { get; init; } = string.Empty;
    public required ScheduleFrequencyTypeDto ScheduleFrequencyType { get; init; }
    public required int FrequencyInterval { get; init; }

    public required int LookaheadLimit { get; init; } = 1;
    public int? OccurrenceCountLimit { get; init; }
    public required ScheduleMisfirePolicyDto MisfirePolicy { get; init; } = ScheduleMisfirePolicyDto.Catchup;

    public TimeSpan? OccurrenceTtl { get; init; }
    
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public required DateTimeOffset ActiveFromUtc { get; init; }
    public DateTimeOffset? ActiveUntilUtc { get; init; }
}