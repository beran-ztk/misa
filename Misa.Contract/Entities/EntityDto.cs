using Misa.Contract.Audit;
using Misa.Contract.Entities.Lookups;
using Misa.Contract.Items;
using Misa.Contract.Main;

namespace Misa.Contract.Entities;

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
    public List<DescriptionDto> Descriptions { get; set; } = new();
    public List<SessionDto> Sessions { get; set; } = new();
}