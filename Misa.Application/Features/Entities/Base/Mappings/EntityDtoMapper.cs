using Misa.Contract.Features.Entities.Base;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Mappings;

public static class EntityDtoMapper
{
    public static Entity ToDomain(this CreateEntityDto createEntity)
        => new
        (
            ownerId: createEntity.OwnerId,
            workflow: Workflow.Task
        );

    public static ReadEntityDto ToReadEntityDto(this Entity entity)
        => new()
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId,
            Workflow = entity.Workflow.ToString(),
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            InteractedAt = entity.InteractedAt
        };

}