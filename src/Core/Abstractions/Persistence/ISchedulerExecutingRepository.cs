using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;

namespace Misa.Core.Abstractions.Persistence;

public interface ISchedulerExecutingRepository
{
    Task<List<(ScheduleExecutionLog Log, Item Schedule)>> GetPendingWithExtensionsAsync(CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
