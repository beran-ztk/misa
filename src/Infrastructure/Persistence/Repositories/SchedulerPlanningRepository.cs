using Npgsql;
using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;
public sealed class SchedulerPlanningRepository(MisaContext context) : ISchedulerPlanningRepository
{
    public Task SaveChangesAsync(CancellationToken ct) => context.SaveChangesAsync(ct);
    
    public Task<List<ScheduleExtension>> GetActiveSchedulesAsync(CancellationToken ct)
        => context.Schedulers
            .Where(s => 
                (s.OccurrenceCountLimit == null || s.OccurrenceCountLimit > 0)
            )
            .ToListAsync(ct);

    public async Task<int> GetExecutionCountPlannedAheadAsync(Guid id, DateTimeOffset utcNow, CancellationToken ct)
        => await context.SchedulerExecutionLogs
            .Where(s => s.SchedulerId.Value == id && s.ScheduledForUtc >= utcNow)
            .CountAsync(ct);

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