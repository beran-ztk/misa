namespace Misa.Contract.Items;

public sealed class UpdateItemDto
{
    public Guid EntityId { get; set; }
    public int? StateId { get; set; }
    public int? PriorityId { get; set; }
    public int? CategoryId { get; set; }
    public string? Title { get; set; }
}
