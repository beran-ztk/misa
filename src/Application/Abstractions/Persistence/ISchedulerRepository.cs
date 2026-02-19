using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task AddAsync(Schedule schedule, CancellationToken ct);
    void Remove(Schedule schedule);
    Task SaveChangesAsync(CancellationToken ct);
}