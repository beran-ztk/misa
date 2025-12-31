namespace Misa.Contract.Scheduling;

public record ScheduleDto(Guid? EntityId, DateTimeOffset StartAtUtc, DateTimeOffset? EndAtUtc);