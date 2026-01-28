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

    public ItemDto Item { get; init; } = null!;
}
