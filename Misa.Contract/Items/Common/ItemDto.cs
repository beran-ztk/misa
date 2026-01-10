namespace Misa.Contract.Items.Common;

public record ItemDto
(
    Guid Id,
    int StateId,
    int PriorityId,
    int CategoryId,
    string Title
);