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

    public async Task<Session> GetTrackedSessionAsync(Guid id)
    {
        return await db.Sessions
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstAsync(i => i.EntityId == id);
    }
    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.EntityId, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
    }
    public async Task AddAsync(SessionSegment segment)
    {
        await db.SessionSegments.AddAsync(segment);
        await db.SaveChangesAsync();
    }
    public async Task<Session> AddSessionAsync(Session session)
    {
        await db.Sessions.AddAsync(session);
        await db.SaveChangesAsync();

        var loaded = await GetTrackedSessionAsync(session.EntityId);
        return loaded
               ?? throw new InvalidOperationException(
                   "Session wurde gespeichert, aber konnte nicht wieder geladen werden");
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
            .Include(e => e.Entity)
                .ThenInclude(e => e.Workflow)

            // Item (alles LEFT JOIN)
            .Include(e => e.Priority)
            .Include(i => i.Priority)
            .Include(e => e.Category)
            
            // Descriptions
            .Include(e => e.Entity)
                .ThenInclude(e => e.Descriptions)
                    .ThenInclude(d => d.Type)
            
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