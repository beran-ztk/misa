using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Messaging;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerRepository(DefaultContext context) : ISchedulerRepository
{
    public Task<Scheduler?> TryGetDeadlineFromTargetItemIdAsync(Guid targetItemId, CancellationToken ct)
        => context.Schedulers
            .Where(s =>
                s.TargetItemId == targetItemId
                && s.ScheduleFrequencyType == ScheduleFrequencyType.Once)
            .FirstOrDefaultAsync(ct);

    public Task AddAsync(Scheduler scheduler, CancellationToken ct)
        => context.Schedulers.AddAsync(scheduler, ct).AsTask();
    public void Remove(Scheduler scheduler)
        => context.Schedulers.Remove(scheduler);
    public Task SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);

    public async Task<List<Outbox>> GetPendingOutboxesAsync(CancellationToken ct)
    {
        return await context.Outbox
            .Where(o => o.EventState == OutboxEventState.Pending)
            .ToListAsync(ct);
    }
}