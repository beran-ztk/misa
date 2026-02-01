using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task<Scheduler?> TryGetDeadlineFromTargetItemIdAsync(Guid targetItemId, CancellationToken ct);
    Task AddAsync(Scheduler scheduler, CancellationToken ct);
    void Remove(Scheduler scheduler);
    Task SaveChangesAsync(CancellationToken ct);
}