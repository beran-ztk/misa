using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface ISchedulerPlanningRepository
{
    Task SaveChangesAsync(CancellationToken ct);
    Task<List<Scheduler>> GetActiveSchedulesAsync(DateTimeOffset now, CancellationToken ct);
    Task<bool> TryAddExecutionLogAsync(SchedulerExecutionLog log, CancellationToken ct);
}