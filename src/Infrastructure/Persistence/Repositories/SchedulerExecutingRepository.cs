using Microsoft.EntityFrameworkCore;
using Misa.Core.Abstractions.Persistence;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerExecutingRepository(MisaContext context) : ISchedulerExecutingRepository
{
    public async Task<List<(ScheduleExecutionLog Log, Item Schedule)>> GetPendingWithExtensionsAsync(CancellationToken ct)
    {
        var logs = await context.SchedulerExecutionLogs
            .Where(x => x.Status == ScheduleExecutionStatus.Pending)
            .ToListAsync(ct);

        if (logs.Count == 0) return [];

        var schedulerIds = logs.Select(l => l.SchedulerId).ToList();

        var extensions = await context.Items.Include(i => i.ScheduleExtension)
            .Where(s => schedulerIds.Contains(s.Id) && s.Workflow == Workflow.Schedule)
            .ToListAsync(ct);

        var extMap = extensions.ToDictionary(e => e.Id);

        return logs
            .Where(l => extMap.ContainsKey(l.SchedulerId))
            .Select(l => (l, extMap[l.SchedulerId]))
            .ToList();
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await context.SaveChangesAsync(ct);
    }
}
