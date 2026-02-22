using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Contract.Items;

public sealed record ItemDto
{
    public required Guid Id { get; init; }
    public required string OwnerId { get; init; }

    public required WorkflowDto Workflow { get; init; }

    public required string Title { get; init; }
    public string? Description { get; init; }

    public required bool IsDeleted { get; init; }
    public required bool IsArchived { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    
    public ItemActivityDto? Activity { get; set; }
    
    public TaskExtensionDto? TaskExtension { get; set; }
}