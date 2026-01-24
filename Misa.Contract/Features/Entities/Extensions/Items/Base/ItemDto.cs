using Misa.Contract.Features.Entities.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ItemDto
{
    public Guid Id { get; set; }
    public int StateId { get; set; }
    public PriorityContract PriorityContract { get; set; }
    public string Title { get; set; } = string.Empty;
    
    public EntityDto Entity { get; set; }
}