using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerExecutingRepository(DefaultContext context) : ISchedulerExecutingRepository
{
    public async Task<List<SchedulerExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct)
    {
        return await context.SchedulerExecutionLogs
            .Include(x => x.Scheduler)
            .Where(x => x.Status == SchedulerExecutionStatus.Pending)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
}