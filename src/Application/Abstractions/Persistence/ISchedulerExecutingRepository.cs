using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Abstractions.Persistence;

public interface ISchedulerExecutingRepository
{
    Task<List<ScheduleExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}