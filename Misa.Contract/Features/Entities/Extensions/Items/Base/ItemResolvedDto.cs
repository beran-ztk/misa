namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public record ItemResolvedDto
(
    Guid Id,
    StateDto State,
    string Priority,
    CategoryDto Category,
    string Title    
);