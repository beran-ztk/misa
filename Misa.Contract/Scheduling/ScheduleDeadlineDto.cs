namespace Misa.Contract.Scheduling;

public record ScheduleDeadlineDto(
    Guid ItemId,
    DateTimeOffset DeadlineAtUtc
);
