using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task AddAsync(ScheduleExtension scheduleExtension, CancellationToken ct);
    void Remove(ScheduleExtension scheduleExtension);
    Task SaveChangesAsync(CancellationToken ct);
}