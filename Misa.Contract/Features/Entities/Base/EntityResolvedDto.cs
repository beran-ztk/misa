namespace Misa.Contract.Features.Entities.Base;

public record EntityResolvedDto
(
    Guid Id,
    Guid? OwnerId,

    WorkflowDto Workflow,
    bool IsDeleted,
    bool IsArchived,

    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? DeletedAt,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset InteractedAt
);
