using Misa.Contract.Features.Entities.Base;
using Misa.Domain.Features.Entities.Base;

namespace Misa.Application.Common.Mappings;

public static class EntityMappings
{
    public static EntityDto ToDto(this Entity entity)
    {
        return new EntityDto
        {
            Id = entity.Id,
            Workflow = entity.Workflow.MapToDto(),
            IsDeleted = entity.IsDeleted,
            IsArchived = entity.IsArchived,

            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            DeletedAt = entity.DeletedAt,
            ArchivedAt = entity.ArchivedAt,
            InteractedAt = entity.InteractedAt
        };
    }
}