using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Infrastructure.Persistence.Context;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ScheduleRepository(DefaultContext db) : IScheduleRepository
{
    public async Task<Scheduler> AddSchedulingRule(CancellationToken ct)
    {
        var item = new Item(1, Priority.None,1,"Default-Schedule-1")
        {
            Entity = new Entity(Workflow.Deadline)
        };
        await db.Items.AddAsync(item, ct);
        
        var schedulingRule = Scheduler.CreateAndInitDefaultValues(item);
        await db.Scheduler.AddAsync(schedulingRule, ct);
        await db.SaveChangesAsync(ct);
        return schedulingRule;
    }
}