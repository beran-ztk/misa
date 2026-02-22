using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Inspector;
public record GetItemDetailsQuery(Guid ItemId);

public sealed class GetItemDetailsHandler(IItemRepository repository)
{
    public async Task<ItemDto?> HandleAsync(GetItemDetailsQuery query, CancellationToken cancellationToken)
    {
        var details = await repository.TryGetItemDetailsAsync(query.ItemId, cancellationToken)
                      ?? throw new DomainNotFoundException("details.not.found", "Item details not found.");

        return details.Workflow switch
        {
            Workflow.Task => details.TaskToItemDto(),
            _ => details.ToDto()
        };
    }
}