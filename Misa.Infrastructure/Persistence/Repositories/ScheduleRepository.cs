using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Scheduling;
using Misa.Infrastructure.Data;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ScheduleRepository(MisaDbContext db) : IScheduleRepository
{
    public async Task Upsert(Schedule schedule)
    {
        var existing = await db.Schedules.SingleOrDefaultAsync(x => x.EntityId == schedule.EntityId);
        
        if (existing is null)
        {
            await db.Schedules.AddAsync(schedule);    
        }
        else
        {
            existing.Reschedule(startAtUtc: schedule.StartAtUtc, endAtUtc: schedule.EndAtUtc);
        }

        await db.SaveChangesAsync();
    }
}