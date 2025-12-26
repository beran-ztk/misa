using Misa.Application.Entities.Mappings;
using Misa.Application.Entities.Repositories;
using Misa.Contract.Entities;

namespace Misa.Application.Entities.Get;

public class GetEntitiesHandler(IEntityRepository repository)
{
    public async Task<List<CreateEntityDto>> GetAllAsync(CancellationToken ct = default)
    {
        List<Misa.Contract.Entities.CreateEntityDto> dto = [];
        
        var entities = await repository.GetAllAsync(ct);

        dto.AddRange(entities.Select(entity => entity.ToDto()));

        return dto;
    }

    public async Task<EntityDto?> GetDetailedEntityAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetDetailedEntityAsync(id, ct);
        return entity?.ToDetailedDto();
    }
}