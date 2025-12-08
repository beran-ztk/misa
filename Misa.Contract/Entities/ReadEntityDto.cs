using Misa.Contract.Entities.Lookups;

namespace Misa.Contract.Entities;

public class ReadEntityDto
{
    public Guid Id { get; set; }
    public Guid? OwnerId { get; set; }
    public WorkflowDto Workflow { get; set; }
    public bool IsDeleted { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }
}