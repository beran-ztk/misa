using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class SchedulerRepository(MisaContext context) : ISchedulerRepository
{
    public Task AddAsync(ScheduleExtension scheduleExtension, CancellationToken ct)
        => context.Schedulers.AddAsync(scheduleExtension, ct).AsTask();
    public void Remove(ScheduleExtension scheduleExtension)
        => context.Schedulers.Remove(scheduleExtension);
    public Task SaveChangesAsync(CancellationToken ct)
        => context.SaveChangesAsync(ct);
}