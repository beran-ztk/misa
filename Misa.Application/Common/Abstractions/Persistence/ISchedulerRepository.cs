using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task<bool> ExistsOnceForTargetAsync(Guid targetItemId, CancellationToken ct);
    Task AddAsync(Scheduler scheduler, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}