using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Messaging;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerRepository
{
    Task AddAsync(Scheduler scheduler, CancellationToken ct);
    void Remove(Scheduler scheduler);
    Task SaveChangesAsync(CancellationToken ct);
    Task<List<Outbox>> GetPendingOutboxesAsync(CancellationToken ct);
}