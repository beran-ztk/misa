using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Exceptions;
using Misa.Application.Items.Repositories;
using Misa.Domain.Audit;
using Misa.Domain.Scheduling;
using Misa.Infrastructure.Data;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Persistence.Repositories;

public class ItemRepository(MisaDbContext db) : IItemRepository
{
    public async Task<Item> GetTrackedItemAsync(Guid id)
    {
        return await db.Items
            .Include(e => e.Entity)
            .FirstAsync(i => i.EntityId == id);
    }
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
    public async Task<Item?> GetTaskAsync(Guid id, CancellationToken ct)
    {
        return await db.Items
            .Include(i => i.Entity)
            .ThenInclude(e => e.Workflow)
            .Include(i => i.State)
            .Include(i => i.Priority)
            .Include(i => i.Category)
            .FirstOrDefaultAsync( i => i.EntityId == id, ct );
    }
    public async Task<List<Item>> GetAllTasksAsync(CancellationToken ct)
    {
        return await db.Items
            .Include(i => i.Entity)
            .ThenInclude(e => e.Workflow)
            .Include(i => i.State)
            .Include(i => i.Priority)
            .Include(i => i.Category)
            .Where(i => i.Entity.WorkflowId == (int)Misa.Domain.Dictionaries.Entities.EntityWorkflows.Task)
            .ToListAsync(ct);
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
    public async Task UpsertDeadlineAsync(Guid itemId, DateTimeOffset dueAtUtc, CancellationToken ct = default)
    {
        var utc = dueAtUtc.ToUniversalTime();
        
        var existing = await db.Set<ScheduledDeadline>()
            .SingleOrDefaultAsync(d => d.ItemId == itemId, ct);

        if (existing is null)
        {
            db.Set<ScheduledDeadline>().Add(new ScheduledDeadline(itemId, utc));
            return;
        }

        existing.Reschedule(utc);
    }
    public async Task RemoveDeadlineAsync(Guid itemId, CancellationToken ct = default)
    {
        var existing = await db
            .Set<ScheduledDeadline>()
            .SingleOrDefaultAsync(d => d.ItemId == itemId, ct);
        
        if (existing is null)
        {
            return;   
        }

        db.Set<ScheduledDeadline>().Remove(existing);
    }
}