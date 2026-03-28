using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Inspector;
public record GetItemDetailsQuery(Guid ItemId);

public sealed class GetItemDetailsHandler(IItemRepository repository)
{
    public async Task<Item?> HandleAsync(GetItemDetailsQuery query, CancellationToken cancellationToken)
    {
        return await repository.TryGetItemDetailsAsync(query.ItemId, cancellationToken)
                      ?? throw new DomainNotFoundException("details.not.found", "Item details not found.");

    }
}