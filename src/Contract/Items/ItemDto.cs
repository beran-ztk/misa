using Misa.Contract.Items.Components.Activity;
using Misa.Contract.Items.Components.Tasks;

namespace Misa.Contract.Items;

public sealed record ItemDto
{
    public Guid Id { get; init; } = Guid.Empty;
    public string OwnerId { get; init; } = string.Empty;

    public WorkflowDto Workflow { get; init; }

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    public bool IsDeleted { get; init; }
    public bool IsArchived { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    
    public ItemActivityDto? Activity { get; set; }
    
    public TaskExtensionDto? TaskExtension { get; set; }
}