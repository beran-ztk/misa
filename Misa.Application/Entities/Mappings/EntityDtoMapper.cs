using Misa.Application.Items.Mappings;
using Misa.Contract.Entities;
using Misa.Contract.Entities.Lookups;
using Misa.Domain.Entities;
using Misa.Application.Main.Mappings;
using Misa.Domain.Audit;

namespace Misa.Application.Entities.Mappings;

public static class EntityDtoMapper
{
    public static Misa.Domain.Entities.Entity ToDomain(this Misa.Contract.Entities.CreateEntityDto createEntity)
        => new
        (
            ownerId: createEntity.OwnerId,
            workflowId: createEntity.WorkflowId
        );
    public static Misa.Contract.Entities.CreateEntityDto ToDto(this Misa.Domain.Entities.Entity entity)
        => new CreateEntityDto()
        {
            Id = entity.Id,
            OwnerId = entity.OwnerId,
            WorkflowId = entity.WorkflowId,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            InteractedAt = entity.InteractedAt
        };

    public static EntityDto ToDetailedDto(this Misa.Domain.Entities.Entity entity)
        => new EntityDto
        {
             Id = entity.Id,
             OwnerId = entity.OwnerId,
             Workflow = entity.Workflow.ToDto(),
             IsDeleted = entity.IsDeleted,
             CreatedAt = entity.CreatedAt,
             UpdatedAt = entity.UpdatedAt,
             InteractedAt = entity.InteractedAt,
             Item = entity.Item?.ToReadItemDto(),
             DescriptionCount = entity.DescriptionCount,
             Descriptions = entity.Descriptions.ToDto(),
             Sessions = entity.Sessions.ToDto(),
             Actions = entity.Actions.ToDto(),
             HasRunningSession = entity.HasRunningSession(),
             HasPausedSession = entity.HasPausedSession(),
             HasActiveSession = entity.HasActiveSession,
             CanStartSession = entity.CanStartSession
        };
    public static ReadEntityDto ToReadEntityDto(this Misa.Domain.Entities.Entity entity)
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

    public static WorkflowDto ToDto(this Misa.Domain.Entities.Workflow workflow)
        => new()
        {
            Id = workflow.Id,
            Name = workflow.Name,
            Synopsis = workflow.Synopsis
        };
}