using Microsoft.EntityFrameworkCore;
using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Items.Components.Schedules;
using Misa.Infrastructure.Persistence.Context;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(MisaContext context) : IItemRepository
{
    public async Task SaveChangesAsync(CancellationToken  ct = default)
        => await context.SaveChangesAsync(ct);

    public async Task AddAsync(Item item, CancellationToken ct)
    {
        await context.Items.AddAsync(item, ct);
    }

    public async Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.StopAutomatically == true
                && s.State != SessionState.Ended
                && s.PlannedDuration != null)
            .Include(s => s.Segments)
            .ToListAsync(ct);
    }

    public async Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s => s.State != SessionState.Ended
            && s.Segments.Any(
                seg => seg.EndedAtUtc == null 
                       && seg.StartedAtUtc <= oldestDateAllowed)
            )
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .ToListAsync(ct);
    }

    public async Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Ended) 
            .Include(s => s.Segments)
            .OrderByDescending(s => s.CreatedAtUtc)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && (s.State == SessionState.Running
                || s.State == SessionState.Paused)) 
            .Include(s => s.Segments)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Running)
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }  
    public async Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId.Value == id
                && s.State == SessionState.Paused)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await context.Sessions.AddAsync(session, ct);
    }
    
    public async Task AddAsync(ScheduleExtension scheduleExtension, CancellationToken ct)
    {
        await context.Schedulers.AddAsync(scheduleExtension, ct);
    }
    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .FirstOrDefaultAsync(e => e.Id == new ItemId(id), ct);
    }
    
    public async Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .SingleOrDefaultAsync(i => i.Id == new ItemId(id), ct);
    }

    public async Task<List<Item>> GetSchedulingRulesAsync(CancellationToken ct)
    {
        return await context.Items
            .Include(s => s.Activity)
            .Include(s => s.ScheduleExtension)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}