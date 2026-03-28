using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record GetZettelQuery(Guid Id);

public sealed class GetZettelHandler(IItemRepository repository)
{
    public async Task<Item?> HandleAsync(GetZettelQuery query, CancellationToken ct)
    {
        var item = await repository.TryGetZettelAsync(query.Id, ct);
        
        return item;
    }
}
