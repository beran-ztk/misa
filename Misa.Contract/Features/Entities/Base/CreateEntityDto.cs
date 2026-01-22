namespace Misa.Contract.Features.Entities.Base;

public class CreateEntityDto
{
    public Guid Id { get; set; }
    public string Workflow { get; set; } = null!;
    public bool IsDeleted { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }
}