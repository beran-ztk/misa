using Misa.Domain.Items.Components.Schedules;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerExecutingRepository
{
    Task<List<ScheduleExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}