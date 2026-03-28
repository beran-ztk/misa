using Misa.Contract.Items.Components.Zettelkasten;
using Misa.Core.Common.Abstractions.Persistence;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record GetZettelQuery(Guid Id);

public sealed class GetZettelHandler(IItemRepository repository)
{
    public async Task<ZettelDto?> HandleAsync(GetZettelQuery query, CancellationToken ct)
    {
        var item = await repository.TryGetZettelAsync(query.Id, ct);
        if (item is null) return null;

        return new ZettelDto(item.Id.Value, item.Title, item.ZettelExtension!.Content, item.CreatedAt, item.ModifiedAt);
    }
}
