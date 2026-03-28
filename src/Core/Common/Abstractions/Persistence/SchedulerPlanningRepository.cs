using Microsoft.EntityFrameworkCore;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence.Context;
using Npgsql;

namespace Misa.Core.Common.Abstractions.Persistence;
public sealed class SchedulerPlanningRepository(MisaContext context)
{
    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
    
    public Task<List<Item>> GetActiveSchedulesAsync(CancellationToken ct)
        => context.Items
            .Include(i => i.ScheduleExtension)
            .Where(s => s.ScheduleExtension != null && !s.IsDeleted && !s.IsArchived &&
                (s.ScheduleExtension.OccurrenceCountLimit == null || s.ScheduleExtension.OccurrenceCountLimit > 0)
            )
            .ToListAsync(ct);

    public async Task<int> GetExecutionCountPlannedAheadAsync(Guid id, CancellationToken ct)
    {
        var utcNow = DateTimeOffset.UtcNow;
        return await context.SchedulerExecutionLogs
            .Where(s => s.SchedulerId == new ItemId(id) && s.ScheduledForUtc >= utcNow)
            .CountAsync(ct);
    }

    public async Task<bool> TryAddExecutionLogAsync(ScheduleExecutionLog log, CancellationToken ct)
    {
        context.SchedulerExecutionLogs.Add(log);

        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException {SqlState:PostgresErrorCodes.UniqueViolation})
        {
            context.Entry(log).State = EntityState.Detached;
            return false;
        }
    }
}