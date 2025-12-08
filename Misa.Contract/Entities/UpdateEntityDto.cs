namespace Misa.Contract.Entities;

public class UpdateEntityDto
{
    public bool? IsDeleted { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? InteractedAt { get; set; }
}