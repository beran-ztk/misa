using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Entities.Mappings;
using Misa.Contract.Entities;

namespace Misa.Application.Entities.Queries.GetSingleDetailedEntity;

public class GetSingleDetailedEntityHandler(IEntityRepository repository)
{
    public async Task<EntityDto?> Handle(Guid id)
    {
        var entity = await repository.GetDetailedEntityAsync(id);
        return entity?.ToDetailedDto();
    }
}