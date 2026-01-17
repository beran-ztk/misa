namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public record ItemResolvedDto
(
    Guid Id,
    StateDto State,
    PriorityDto Priority,
    CategoryDto Category,
    string Title    
);