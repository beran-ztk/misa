namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public class ItemDto
{
    public required Guid Id { get; init; }
    public required ItemStateDto State { get; init; }
    public required PriorityDto Priority { get; init; }
    public required string Title { get; init; }
    
    
    public static ItemDto Empty() => new()
    {
        Id = Guid.Empty,
        State = default,
        Priority = default,
        Title = string.Empty
    };
}