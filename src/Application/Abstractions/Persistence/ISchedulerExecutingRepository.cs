using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Messaging;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerExecutingRepository
{
    Task AddOutboxMessageAsync(Outbox outbox, CancellationToken ct);
    Task<List<SchedulerExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}