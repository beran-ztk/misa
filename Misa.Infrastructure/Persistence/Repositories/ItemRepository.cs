using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Scheduling;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Infrastructure.Persistence.Context;
using Item = Misa.Domain.Features.Entities.Extensions.Items.Base.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(DefaultContext context) : IItemRepository
{
    public async Task SaveChangesAsync(CancellationToken  ct = default)
        => await context.SaveChangesAsync(ct);

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

    public async Task<Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task?> TryGetTaskAsync(Guid id, CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
            .ThenInclude(i => i.State)
            .Include(t => t.Item)
            .ThenInclude(i => i.Entity)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken: ct);
    }

    public async Task<List<Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task>> GetTasksAsync(CancellationToken ct)
    {
        return await context.Tasks
            .Include(t => t.Item)
                .ThenInclude(i => i.State)
            .Include(t => t.Item)
                .ThenInclude(i => i.Entity)
            .OrderByDescending(t => t.Item.Entity.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId == id
                && s.State == SessionState.Ended) 
            .Include(s => s.Segments)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId == id
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
                s.ItemId == id
                && s.State == SessionState.Running)
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }  
    public async Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await context.Sessions
            .Where(s =>
                s.ItemId == id
                && s.State == SessionState.Paused)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await context.Items.AddAsync(item, ct);
        await context.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.Id, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
    }

    public async Task AddAsync(Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task task, CancellationToken ct)
    {
        await context.Tasks.AddAsync(task, ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await context.Sessions.AddAsync(session, ct);
    }
    
    public async Task AddAsync(Scheduler scheduler, CancellationToken ct)
    {
        await context.Schedulers.AddAsync(scheduler, ct);
    }
    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(i => i.State)
            
            .Include(e => e.Entity)
                .ThenInclude(e => e.Descriptions)
            
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<Scheduler?> TryGetDeadlineByItemIdAsync(Guid id, CancellationToken ct)
    {
        return context.Schedulers
            .Where(s =>
                s.TargetItemId == id &&
                s.ScheduleFrequencyType == ScheduleFrequencyType.Once)
            .OrderByDescending(s => s.ActiveFromUtc) // falls es je mehrere gab
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default)
    {
        return await context.Items
            .Include(i => i.Entity)
            .ThenInclude(e => e.Workflow)
            .Include(i => i.State)
            .SingleOrDefaultAsync(i => i.Id == entityId, ct);
    }
    
    
    public async Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct)
    {
        return await context.Items
            .Include(e => e.Entity)
            .SingleOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<List<Scheduler>> GetSchedulingRulesAsync(CancellationToken ct)
    {
        return await context.Schedulers
            .Include(s => s.Item)
            .ThenInclude(i => i.Entity)
            .ToListAsync(ct);
    }
}