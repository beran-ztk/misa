using Microsoft.EntityFrameworkCore;
using Misa.Application.Items.Repositories;
using Misa.Infrastructure.Data;
using Item = Misa.Domain.Items.Item;

namespace Misa.Infrastructure.Items;

public class ItemRepository(MisaDbContext db) : IItemRepository
{
    public async Task<Item> AddAsync(Item item, CancellationToken ct = default)
    {
        await db.Items.AddAsync(item, ct);
        await db.SaveChangesAsync(ct);
        var loaded = await LoadAsync(item.EntityId, ct);
        
        return loaded 
               ?? throw new InvalidOperationException("Item wurde gespeichert, konnte aber nicht wieder geladen werden.");
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