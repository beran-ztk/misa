using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerRepository(DefaultContext context) : ISchedulerRepository
{
    public Task<bool> ExistsOnceForTargetAsync(Guid targetItemId, CancellationToken ct)
        => context.Schedulers.AnyAsync(s =>
                s.TargetItemId == targetItemId
                && s.ScheduleFrequencyType == ScheduleFrequencyType.Once,
            ct);

    public Task AddAsync(Scheduler scheduler, CancellationToken ct)
        => context.Schedulers.AddAsync(scheduler, ct).AsTask();

    public Task SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);
}