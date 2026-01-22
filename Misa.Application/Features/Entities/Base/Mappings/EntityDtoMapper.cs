using Misa.Contract.Features.Entities.Base;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Features.Entities.Base.Mappings;

public static class EntityDtoMapper
{
    public static Entity ToDomain(this CreateEntityDto createEntity)
        => new
        (
            ownerId: createEntity.OwnerId,
            workflowId: createEntity.WorkflowId
        );

    public static ReadEntityDto ToReadEntityDto(this Entity entity)
        => new()
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId,
            Workflow = entity.Workflow.ToDto(),
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            InteractedAt = entity.InteractedAt
        };

    public static WorkflowDto ToDto(this Workflow workflow)
        => new()
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Synopsis = workflow.Synopsis
        };
}