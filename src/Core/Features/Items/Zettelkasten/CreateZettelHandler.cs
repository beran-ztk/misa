using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record CreateZettelCommand(string Title, Guid ParentId);

public sealed class CreateZettelHandler(ItemRepository repository)
{
    public async Task HandleAsync(CreateZettelCommand command)
    {
        var zettel = Item.CreateZettel(
            id: new ItemId(Guid.NewGuid()),
            title: command.Title,
            createdAtUtc: DateTimeOffset.UtcNow,
            parentId: new ItemId(command.ParentId)
        );

        await repository.AddAsync(zettel, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
