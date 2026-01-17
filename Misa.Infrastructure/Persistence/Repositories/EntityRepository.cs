using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Features.Descriptions;
using Misa.Infrastructure.Persistence.Context;
using Entity = Misa.Domain.Features.Entities.Base.Entity;

namespace Misa.Infrastructure.Persistence.Repositories;

public class EntityRepository(DefaultContext db) : IEntityRepository
{
    public async Task SaveChangesAsync() => await db.SaveChangesAsync();
    public async Task<Entity> GetTrackedEntityAsync(Guid id)
        => await db.Entities.FirstAsync(e => e.Id == id);


    public async Task<Entity> AddAsync(Entity entity, CancellationToken ct)
    {
        await db.Entities.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task AddDescriptionAsync(Description description, CancellationToken ct)
    {
        await db.Descriptions.AddAsync(description, ct);
    }
    public async Task<List<Entity>> GetAllAsync(CancellationToken ct)
    {
        return await db.Entities.ToListAsync(ct);
    }

    public async Task<Entity?> GetDetailedEntityAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Entities
            .Include(e => e.Workflow)

            // Item (alles LEFT JOIN)
            .Include(e => e.Item)
                .ThenInclude(i => i.State)
            .Include(e => e.Item)
                .ThenInclude(i => i.Priority)
            .Include(e => e.Item)
                .ThenInclude(i => i.Category)
            .Include(e => e.Item)
                .ThenInclude(i => i.ScheduledDeadline)
            
            // Descriptions
            .Include(e => e.Descriptions)
            
            // Actions
            .Include(e => e.Actions)
                .ThenInclude(a => a.Type)
            
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}