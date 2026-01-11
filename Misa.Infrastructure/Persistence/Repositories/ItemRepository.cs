using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Exceptions;
using Misa.Domain.Audit;
using Misa.Domain.Entities;
using Misa.Domain.Scheduling;
using Misa.Infrastructure.Data;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(MisaDbContext db) : IItemRepository
{
    public async Task SaveChangesAsync(CancellationToken  ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<Session?> TryGetActiveSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && (s.StateId == (int)Domain.Dictionaries.Audit.SessionState.Running
                || s.StateId == (int)Domain.Dictionaries.Audit.SessionState.Paused)) 
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Session?> TryGetRunningSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && s.StateId == (int)Domain.Dictionaries.Audit.SessionState.Running)
            .Include(s => s.Segments.Where(seg => seg.EndedAtUtc == null))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }  
    public async Task<Session?> TryGetPausedSessionByItemIdAsync(Guid id, CancellationToken ct)
    {
        return await db.Sessions
            .Where(s =>
                s.ItemId == id
                && s.StateId == (int)Domain.Dictionaries.Audit.SessionState.Paused)
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.EntityId, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
    }
    public async Task AddAsync(SessionSegment segment, CancellationToken ct)
    {
        await db.SessionSegments.AddAsync(segment, ct);
    }
    public async Task AddAsync(Session session, CancellationToken ct)
    {
        await db.Sessions.AddAsync(session, ct);
    }
    public async Task<List<Item>> TryGetTasksAsync(CancellationToken ct)
    {
        return await db.Items
            .Include(e => e.Entity)
            .Include(i => i.State)
            .Include(i => i.Priority)
            .Include(i => i.Category)
            .Where(i => i.Entity.WorkflowId == (int)Domain.Dictionaries.Entities.EntityWorkflows.Task)
            .ToListAsync(ct);
    }

    public async Task<Item?> TryGetItemDetailsAsync(Guid id, CancellationToken ct)
    {
        return await db.Items
            .Include(i => i.State)
            .Include(i => i.Priority)
            .Include(i => i.Category)
            
            .Include(e => e.Entity)
                .ThenInclude(e => e.Workflow)
            .Include(e => e.Entity)
                .ThenInclude(e => e.Descriptions)
            
            .FirstOrDefaultAsync(e => e.EntityId == id, ct);
    }

    public async Task<Item?> LoadAsync(Guid entityId, CancellationToken ct = default)
    {
        return await db.Items
            .Include(i => i.Entity)
            .ThenInclude(e => e.Workflow)
            .Include(i => i.State)
            .Include(i => i.Priority)
            .Include(i => i.Category)
            .SingleOrDefaultAsync(i => i.EntityId == entityId, ct);
    }
    
    
    public async Task<Item?> TryGetItemAsync(Guid id, CancellationToken ct)
    {
        return await db.Items
            .Include(e => e.Entity)
            .SingleOrDefaultAsync(i => i.EntityId == id, ct);
    }
    public async Task<ScheduledDeadline?> TryGetScheduledDeadlineForItemAsync(Guid itemId, CancellationToken ct)
    {
        return await db.Deadlines
            .SingleOrDefaultAsync(d => d.ItemId == itemId, ct);
    }
    public async Task AddDeadlineAsync(ScheduledDeadline deadline)
    {
        await db.Deadlines.AddAsync(deadline);
    }
    public Task RemoveScheduledDeadlineAsync(ScheduledDeadline obj, CancellationToken ct)
    {
        db.Deadlines.Remove(obj);
        
        return Task.CompletedTask;
    }
}