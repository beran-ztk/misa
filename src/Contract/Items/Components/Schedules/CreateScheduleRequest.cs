namespace Misa.Contract.Items.Components.Schedules;

public sealed class CreateScheduleRequest
{
    public required string Title { get; init; } = string.Empty;
    public required string Description { get; init; } = string.Empty;
    public Guid? TargetItemId { get; init; }
    public required ScheduleFrequencyTypeDto ScheduleFrequencyType { get; init; }
    public required int FrequencyInterval { get; init; }

    public required int LookaheadLimit { get; init; } = 1;
    public int? OccurrenceCountLimit { get; init; }
    public required ScheduleMisfirePolicyDto MisfirePolicy { get; init; } = ScheduleMisfirePolicyDto.Catchup;

    public TimeSpan? OccurrenceTtl { get; init; }
    public required ScheduleActionTypeDto ActionType { get; init; }
    public string? Payload { get; init; }
    
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    public required DateTimeOffset ActiveFromLocal { get; init; }
    public DateTimeOffset? ActiveUntilLocal { get; init; }
    
    public int[]? ByDay { get; init; }        // 1..7 (Mo..So)
    public int[]? ByMonthDay { get; init; }   // 1..31
    public int[]? ByMonth { get; init; }      // 1..12
}