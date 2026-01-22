using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Features.Descriptions;

namespace Misa.Domain.Features.Entities.Base;

public class Entity
{
    private Entity () {}

    public Entity(Guid? ownerId, int workflowId)
    {
        OwnerId = ownerId;
        WorkflowId = workflowId;
        
        CreatedAt = DateTimeOffset.UtcNow;
        InteractedAt = DateTimeOffset.UtcNow;
    }
    
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public int WorkflowId { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsArchived { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }

    public Workflow Workflow { get;  set; }
    public Item? Item { get; set; }
    
    public ICollection<Description> Descriptions { get; init; } = new List<Description>();
    
    
    public ICollection<AuditChange> Changes { get; init; } = new List<AuditChange>();

   
    public void Interact() => InteractedAt =  DateTimeOffset.UtcNow;
    public void Update() => UpdatedAt = DateTimeOffset.UtcNow;

    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
    }
    public void Archive()
    {
        IsArchived = true;
        ArchivedAt = DateTimeOffset.UtcNow;
    }
}