using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Extensions;
using Misa.Domain.Scheduling;
using Misa.Infrastructure.Data;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ScheduleRepository(MisaDbContext db) : IScheduleRepository
{
    public async Task Upsert(Guid childItem, Schedule schedule)
    {
        var existing = await db.Schedules.SingleOrDefaultAsync(x => x.EntityId == schedule.EntityId);
        
        if (existing is null)
        {
            await db.Schedules.AddAsync(schedule);
            await db.SaveChangesAsync();
        }
        else
        {
            existing.Reschedule(startAtUtc: schedule.StartAtUtc, endAtUtc: schedule.EndAtUtc);
            await db.SaveChangesAsync();
            return;
        }

        var relation = new Relations(schedule.EntityId, )
        
        
    }

    public async Task<bool> HasDeadline(Guid entityId)
    {
        var rs = await db.Relations.SingleAsync(r => r.ParentId)
    }
}