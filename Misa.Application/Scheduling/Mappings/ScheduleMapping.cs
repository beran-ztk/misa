using Misa.Contract.Scheduling;
using Misa.Domain.Scheduling;

namespace Misa.Application.Scheduling.Mappings;

public static class ScheduleMapping
{
    public static Schedule ToDomain(ScheduleDto scheduleDto) 
        => new
        (
            entityId: scheduleDto.EntityId,
            startAtUtc: scheduleDto.StartAtUtc,
            endAtUtc: scheduleDto.EndAtUtc
        );

    public static ScheduleDto ToDto(Schedule schedule)
        => new 
        (
            EntityId: schedule.EntityId, 
            StartAtUtc: schedule.StartAtUtc, 
            EndAtUtc: schedule.EndAtUtc
        );
}