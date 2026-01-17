using Misa.Contract.Features.Actions;
using Misa.Contract.Features.Entities.Extensions.Items.Base;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;

namespace Misa.Contract.Features.Entities.Base;

public class EntityDto
{
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }

    public WorkflowDto Workflow { get; set; } = null!;
    public bool IsDeleted { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }

    public ReadItemDto? Item { get; set; }
    public int DescriptionCount { get; set; } = 0;
    public List<SessionResolvedDto> Sessions { get; set; } = new();
    public List<ActionDto> Actions { get; set; } = new();
    public bool HasRunningSession { get; set; }
    public bool HasPausedSession { get; set; }
    public bool HasActiveSession { get; set; }
    public bool CanStartSession { get; set; }
    
}