namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public sealed class CreateItemDto
{
    public Guid? OwnerId { get; set; }
    public int StateId { get; set; }
    public int PriorityId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = null!;
}
