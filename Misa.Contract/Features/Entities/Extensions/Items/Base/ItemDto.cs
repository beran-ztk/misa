namespace Misa.Contract.Features.Entities.Extensions.Items.Base;

public record ItemDto
(
    Guid Id,
    int StateId,
    int PriorityId,
    int CategoryId,
    string Title
);