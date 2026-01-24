namespace Misa.Contract.Features.Entities.Base;

public class EntityDto
{
    public required Guid Id { get; init; }
    public required WorkflowContract Workflow { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool IsArchived { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? UpdatedAt { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }
    public required DateTimeOffset? ArchivedAt { get; init; }
    public required DateTimeOffset? InteractedAt { get; init; }
}