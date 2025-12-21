using System.Security.Cryptography.X509Certificates;
using Misa.Domain.Audit;
using Misa.Domain.Items;
using Misa.Domain.Main;
using Action = Misa.Domain.Audit.Action;

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
    public int DescriptionCount => Descriptions.Count;
    public ICollection<Description> Descriptions { get; set; } = new List<Description>();
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Domain.Audit.Action> Actions { get; set; } = new List<Action>(); 
    
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