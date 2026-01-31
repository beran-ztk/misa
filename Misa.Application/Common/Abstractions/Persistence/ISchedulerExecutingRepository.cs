using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface ISchedulerExecutingRepository
{
    Task<List<SchedulerExecutionLog>> GetPendingExecutionLogsAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}