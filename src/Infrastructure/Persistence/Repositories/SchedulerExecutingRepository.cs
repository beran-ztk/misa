using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerExecutingRepository(MisaContext context) : ISchedulerExecutingRepository
{
    public async Task<List<ScheduleExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct)
    {
        return await context.SchedulerExecutionLogs
            .Include(x => x.Schedule)
            .Where(x => x.Status == ScheduleExecutionStatus.Pending)
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
}