using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Base.Mappings;
using Misa.Contract.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Queries;

public class GetSingleDetailedEntityHandler(IEntityRepository repository)
{
    public async Task<EntityDto?> Handle(Guid id)
    {
        var entity = await repository.GetDetailedEntityAsync(id);
        return entity?.ToDetailedDto();
    }
}