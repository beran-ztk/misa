using Misa.Application.Abstractions.Persistence;
using Misa.Application.Mappings;
using Misa.Contract.Items;

namespace Misa.Application.Features.Items.Inspector;
public record GetItemDetailsQuery(Guid ItemId);

public sealed class GetItemDetailsHandler(IItemRepository repository)
{
    public async Task<ItemDto?> HandleAsync(GetItemDetailsQuery query, CancellationToken cancellationToken)
    {
        var details = await repository.TryGetItemDetailsAsync(query.ItemId, cancellationToken);

        return details?.ToDto();
    }
}