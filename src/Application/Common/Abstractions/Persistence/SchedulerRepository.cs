using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence;

namespace Misa.Core.Common.Abstractions.Persistence;

public class SchedulerRepository(Context context)
{
    public Task AddAsync(Schedule schedule, CancellationToken ct)
        => context.Schedulers.AddAsync(schedule, ct).AsTask();
    public void Remove(Schedule schedule)
        => context.Schedulers.Remove(schedule);
    public Task SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);
}