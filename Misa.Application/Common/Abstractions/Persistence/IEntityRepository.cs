using Misa.Domain.Features.Entities.Base;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Application.Common.Abstractions.Persistence;

public interface IEntityRepository
{
    public Task<Entity> GetTrackedEntityAsync(Guid id);
    public Task SaveChangesAsync();
    public Task<Entity> AddAsync(Entity entity, CancellationToken ct);
    public Task<List<Entity>> GetAllAsync(CancellationToken ct);
    public Task AddDescriptionAsync(Description description, CancellationToken ct);

}