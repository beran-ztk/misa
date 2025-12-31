using Misa.Domain.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IScheduleRepository
{
    Task Upsert(Schedule schedule, Guid childItem);
    Task<bool> HasDeadline(Guid entityId);
}