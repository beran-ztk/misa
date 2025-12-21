using Microsoft.EntityFrameworkCore;
using Misa.Application.Items.Repositories;
using Misa.Domain.Audit;
using Misa.Infrastructure.Data;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Items;

public class ItemRepository(MisaDbContext db) : IItemRepository
{
    public async Task<Item> GetTrackedItemAsync(Guid id)
        => await db.Items.Include(e => e.Entity).FirstAsync(i => i.EntityId == id);
    public async Task SaveChangesAsync(CancellationToken  ct = default)
        => await db.SaveChangesAsync(ct);

    public async Task<Session> GetTrackedSessionAsync(Guid id)
    {
        return await db.Sessions.FirstAsync(i => i.EntityId == id && i.EndedAtUtc == null);
    }
    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.EntityId, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
    }

    public async Task AddAsync(Session session)
    {
        await db.Sessions.AddAsync(session);
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


}