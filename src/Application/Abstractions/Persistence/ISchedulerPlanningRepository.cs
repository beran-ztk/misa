using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerPlanningRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task<List<Item>> GetActiveSchedulesAsync(CancellationToken ct);
    Task<int> GetExecutionCountPlannedAheadAsync(Guid id, DateTimeOffset now, CancellationToken ct);
    Task<bool> TryAddExecutionLogAsync(ScheduleExecutionLog log, CancellationToken ct);
}