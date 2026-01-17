using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Base.Mappings;
using Misa.Contract.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Queries;

public class GetEntitiesHandler(IEntityRepository repository)
{
    public async Task<List<CreateEntityDto>> GetAllAsync(CancellationToken ct = default)
    {
        List<CreateEntityDto> dto = [];
        
        var entities = await repository.GetAllAsync(ct);

        dto.AddRange(entities.Select(entity => entity.ToDto()));

        return dto;
    }

    
}