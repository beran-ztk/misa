using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Commands;

public class AddEntityHandler(IEntityRepository repository)
{
    public async Task<Entity> AddAsync(Entity entity, CancellationToken ct = default)
    {
        return await repository.AddAsync(entity, ct);
    }
}