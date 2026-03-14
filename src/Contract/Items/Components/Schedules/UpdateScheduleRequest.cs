namespace Misa.Contract.Items.Components.Schedules;

public record UpdateScheduleRequest(
    string? Title,
    string? Description,
    ScheduleMisfirePolicyDto? MisfirePolicy,
    int? LookaheadLimit,
    int? OccurrenceCountLimit,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateTimeOffset? ActiveUntilUtc);
