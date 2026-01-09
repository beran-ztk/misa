using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Entities.Mappings;
using Misa.Contract.Entities;

namespace Misa.Application.Entities.Queries;

public class GetEntitiesHandler(IEntityRepository repository)
{
    public async Task<List<CreateEntityDto>> GetAllAsync(CancellationToken ct = default)
    {
        List<Misa.Contract.Entities.CreateEntityDto> dto = [];
        
        var entities = await repository.GetAllAsync(ct);

        dto.AddRange(entities.Select(entity => entity.ToDto()));

        return dto;
    }

    
}