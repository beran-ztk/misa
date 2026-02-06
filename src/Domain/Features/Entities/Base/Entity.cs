using Misa.Domain.Features.Audit;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Features;

namespace Misa.Domain.Features.Entities.Base;

public class Entity
{
    private Entity () { }

    private Entity(Guid id, Workflow workflow, DateTimeOffset createdAt)
    {
        Id = id;
        Workflow = workflow;
        CreatedAt = createdAt;
    }

    public static Entity Create(Guid id, Workflow workflow, DateTimeOffset createdAtUtc)
    {
        return new Entity(id, workflow, createdAtUtc);
    }
    public Guid Id { get; init; }
    public Workflow Workflow { get; init; }
    public bool IsDeleted { get; init; }
    public bool IsArchived { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; init; }
    public DateTimeOffset? ArchivedAt { get; init; }
    public DateTimeOffset? InteractedAt { get; init; }
    
    public ICollection<AuditChange> Changes { get; init; } = new List<AuditChange>();
    public ICollection<Description> Descriptions { get; init; } = new List<Description>();

    public void Update(DateTimeOffset utcNow) 
        => UpdatedAt = utcNow;
}