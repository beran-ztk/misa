namespace Misa.Contract.Features.Entities.Extensions.Items.Features.Scheduler;

public sealed class AddScheduleDto
{
    public string Title { get; init; } = string.Empty;
    public required ScheduleFrequencyTypeContract ScheduleFrequencyType { get; init; }
    public required int FrequencyInterval { get; init; }

    public int? OccurrenceCountLimit { get; init; }
    public ScheduleMisfirePolicyContract MisfirePolicy { get; init; } = ScheduleMisfirePolicyContract.Catchup;

    public TimeSpan? OccurrenceTtl { get; init; }
    
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public DateTimeOffset ActiveFromUtc { get; init; }
    public DateTimeOffset? ActiveUntilUtc { get; init; }
}