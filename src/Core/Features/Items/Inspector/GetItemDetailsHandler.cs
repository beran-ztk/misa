using Misa.Contract.Items;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Mappings;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Inspector;
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
            Workflow.Schedule => details.ScheduleToItemDto(),
            _ => details.ToDto()
        };
    }
}