using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Items.Components.Zettelkasten;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record GetZettelsQuery(Guid? TopicId);

public sealed class GetZettelsHandler(IItemRepository repository)
{
    public async Task<List<ZettelDto>> HandleAsync(GetZettelsQuery query, CancellationToken ct)
    {
        var items = await repository.GetZettelsAsync(query.TopicId, ct);

        return items
            .Select(i => new ZettelDto(i.Id.Value, i.Title, i.ZettelExtension!.Content))
            .ToList();
    }
}
