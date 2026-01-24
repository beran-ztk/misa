using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Deadlines;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;
using Misa.Infrastructure.Persistence.Context;
using Item = Misa.Domain.Features.Entities.Extensions.Items.Base.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(DefaultContext db) : IItemRepository
{
    public async Task<List<Session>> GetActiveSessionsWithAutostopAsync(CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.StopAutomatically == true
                && s.State != SessionState.Ended
                && s.PlannedDuration != null)
            .Include(s => s.Segments)
            .ToListAsync(ct);
    }

    public async Task<List<Session>> GetInactiveSessionsAsync(DateTimeOffset oldestDateAllowed, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s => s.State != SessionState.Ended
            && s.Segments.Any(
                seg => seg.EndedAtUtc == null 
                       && seg.StartedAtUtc <= oldestDateAllowed)
            )
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .ToListAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken  ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<Session?> TryGetLatestCompletedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && s.State == SessionState.Ended) 
            .Include(s => s.Segments)
            .Include(s => s.State)
            .Include(s => s.Efficiency)
            .Include(s => s.Concentration)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && (s.State == SessionState.Running
                || s.State == SessionState.Paused)) 
            .Include(s => s.Segments)
            .Include(s => s.State)
            .Include(s => s.Efficiency)
            .Include(s => s.Concentration)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && s.State == SessionState.Running)
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }  
    public async Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && s.State == SessionState.Paused)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.Id, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
    }

    public async Task AddAsync(Domain.Features.Entities.Extensions.Items.Extensions.Tasks.Task task, CancellationToken ct)
    {
        await db.Tasks.AddAsync(task, ct);
    }

    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await db.Sessions.AddAsync(session, ct);
    }
    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        return await db.Items
            .Include(i => i.State)
            
            .Include(e => e.Entity)
                .ThenInclude(e => e.Workflow)
            .Include(e => e.Entity)
                .ThenInclude(e => e.Descriptions)
            
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default)
    {
        return await db.Items
            .Include(i => i.Entity)
            .ThenInclude(e => e.Workflow)
            .Include(i => i.State)
            .SingleOrDefaultAsync(i => i.Id == entityId, ct);
    }
    
    
    public async Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct)
    {
        return await db.Items
            .Include(e => e.Entity)
            .SingleOrDefaultAsync(i => i.Id == id, ct);
    }
    public async Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct)
    {
        return await db.Deadlines
            .SingleOrDefaultAsync(d => d.ItemId == itemId, ct);
    }
    public async Task AddDeadlineAsync(ScheduledDeadline deadline, CancellationToken ct = default)
    {
        await db.Deadlines.AddAsync(deadline, ct);
    }
    public Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct)
    {
        db.Deadlines.Remove(obj);
        
        return Task.CompletedTask;
    }
}