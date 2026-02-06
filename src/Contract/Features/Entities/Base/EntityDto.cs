using Misa.Contract.Features.Entities.Features;

namespace Misa.Contract.Features.Entities.Base;

public class EntityDto
{
    public required Guid Id { get; init; }
    public required WorkflowDto Workflow { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool IsArchived { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset? UpdatedAt { get; init; }
    public required DateTimeOffset? DeletedAt { get; init; }
    public required DateTimeOffset? ArchivedAt { get; init; }
    public required DateTimeOffset? InteractedAt { get; init; }
    public ICollection<DescriptionDto> Descriptions { get; init; } = [];
    public static EntityDto Empty() => new()
    {
        Id = Guid.Empty,
        Workflow = default!,
        IsDeleted = false,
        IsArchived = false,
        CreatedAt = DateTimeOffset.MinValue,
        UpdatedAt = null,
        DeletedAt = null,
        ArchivedAt = null,
        InteractedAt = null
    };
}