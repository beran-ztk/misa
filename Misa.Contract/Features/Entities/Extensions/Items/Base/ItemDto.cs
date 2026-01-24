using Misa.Contract.Features.Entities.Base;

namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ItemDto
{
    public required Guid Id { get; init; }
    public required int StateId { get; init; }
    public required  PriorityContract Priority { get; init; }
    public required string Title { get; init; }
    
    public required EntityDto Entity { get; init; }
}