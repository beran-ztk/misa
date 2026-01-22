namespace Misa.Contract.Features.Entities.Base;

public class ReadEntityDto
{
    public Guid Id { get; set; }
    public string Workflow { get; set; }
    public bool IsDeleted { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset InteractedAt { get; set; }
}