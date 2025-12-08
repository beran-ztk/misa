namespace Misa.Domain.Entities;

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
    
    public Guid Id { get; private set; }
    public Guid? OwnerId { get; private set; }
    public int WorkflowId { get; private set; }
    public bool IsDeleted { get; private set; }
    
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset InteractedAt { get; private set; }

    public Workflow Workflow { get; private set; }
    
    public void Interact() => InteractedAt =  DateTimeOffset.UtcNow;
    public void Update() => UpdatedAt = DateTimeOffset.UtcNow;
    public void Delete() => IsDeleted = true;
}