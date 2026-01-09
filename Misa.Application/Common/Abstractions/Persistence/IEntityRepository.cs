using Misa.Domain.Entities;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IEntityRepository
{
    public Task<Entity> GetTrackedEntityAsync(Guid id);
    public Task SaveChangesAsync();
    public Task<Misa.Domain.Entities.Entity> AddAsync(Misa.Domain.Entities.Entity entity, CancellationToken ct);
    public Task<List<Misa.Domain.Entities.Entity>> GetAllAsync(CancellationToken ct);
    public Task<Entity?> GetDetailedEntityAsync(Guid id, CancellationToken ct = default);
}