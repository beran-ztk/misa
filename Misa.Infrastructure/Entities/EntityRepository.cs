using Microsoft.EntityFrameworkCore;
using Misa.Infrastructure.Configurations.Ef;

namespace Misa.Infrastructure.Entities;

public class EntityRepository(Misa.Infrastructure.Data.MisaDbContext db) : Misa.Application.Entities.Repositories.IEntityRepository
{
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

    public async Task<Domain.Entities.Entity?> GetDetailedEntityAsync(Guid id, CancellationToken ct)
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
            
            // Descriptions
            .Include(e => e.Descriptions)
                .ThenInclude(d => d.Type)
            
            // Sessions
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Efficiency)
            .Include(e => e.Sessions)
                .ThenInclude(s => s.Concentration)
            
            // Actions
            .Include(e => e.Actions)
                .ThenInclude(a => a.Type)
            
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }
}