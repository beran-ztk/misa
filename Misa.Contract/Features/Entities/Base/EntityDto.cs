namespace Misa.Contract.Features.Entities.Base;

public class EntityDto
{
    public Guid Id { get; set; }
    public WorkflowContract Workflow { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsArchived { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public DateTimeOffset? InteractedAt { get; set; }
}