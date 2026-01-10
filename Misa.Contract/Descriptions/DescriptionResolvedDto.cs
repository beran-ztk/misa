namespace Misa.Contract.Descriptions;

public class DescriptionResolvedDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string Content { get; set; }
    public DescriptionTypeDto Type { get; set; }
    public DateTimeOffset CreatedAtUtc  { get; set; }
}