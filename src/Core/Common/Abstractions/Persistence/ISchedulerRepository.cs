using Misa.Domain.Items.Components.Schedules;

namespace Misa.Core.Common.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task AddAsync(Schedule schedule, CancellationToken ct);
    void Remove(Schedule schedule);
    Task SaveChangesAsync(CancellationToken ct);
}