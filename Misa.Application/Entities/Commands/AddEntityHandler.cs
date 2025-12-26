using Misa.Application.Entities.Repositories;
using Misa.Domain.Entities;

namespace Misa.Application.Entities.Add;

public class AddEntityHandler(IEntityRepository repository)
{
    public async Task<Entity> AddAsync(Entity entity, CancellationToken ct = default)
    {
        return await repository.AddAsync(entity, ct);
    }
}