using Microsoft.EntityFrameworkCore;
using Misa.Application.Common.Abstractions.Persistence;
using Entity = Misa.Domain.Entities.Entity;

namespace Misa.Infrastructure.Persistence.Repositories;

public class EntityRepository(Misa.Infrastructure.Data.MisaDbContext db) : IEntityRepository
{
    public async Task<Entity> GetTrackedEntityAsync(Guid id)
        => await db.Entities.FirstAsync(e => e.Id == id);

    public async Task SaveChangesAsync() => await db.SaveChangesAsync();

    public async Task<Misa.Domain.Entities.Entity> AddAsync(Misa.Domain.Entities.Entity entity, CancellationToken ct)
    {
        await db.Entities.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<List<Misa.Domain.Entities.Entity>> GetAllAsync(CancellationToken ct)
    {
        return await db.Entities.ToListAsync(ct);
    }

    public async Task<Domain.Entities.Entity?> GetDetailedEntityAsync(Guid id, CancellationToken ct = default)
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
            
            // Sessions
            .Include(e => e.Sessions)
                .ThenInclude(s => s.State)
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Efficiency)
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Concentration)
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Segments)
            
            // Actions
            .Include(e => e.Actions)
                .ThenInclude(a => a.Type)
            
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}