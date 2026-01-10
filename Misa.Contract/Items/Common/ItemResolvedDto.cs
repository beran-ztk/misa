using Misa.Contract.Items.Lookups;

namespace Misa.Contract.Items.Common;

public record ItemResolvedDto
(
    Guid Id,
    StateDto State,
    PriorityDto Priority,
    CategoryDto Category,
    string Title    
);