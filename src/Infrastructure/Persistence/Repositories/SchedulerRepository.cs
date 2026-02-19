using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerRepository(MisaContext context) : ISchedulerRepository
{
    public Task AddAsync(Schedule schedule, CancellationToken ct)
        => context.Schedulers.AddAsync(schedule, ct).AsTask();
    public void Remove(Schedule schedule)
        => context.Schedulers.Remove(schedule);
    public Task SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);
}