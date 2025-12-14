namespace Misa.Contract.Main;

public class DescriptionDto
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public int TypeId { get; set; } = 1;
    public string Content { get; set; }
    public DescriptionTypeDto Type { get; set; }
    public DateTimeOffset CreatedAtUtc  { get; set; }
}