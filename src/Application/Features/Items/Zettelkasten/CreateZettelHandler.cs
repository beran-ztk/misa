using Misa.Core.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record CreateZettelCommand(string Title, Guid ParentId);

public sealed class CreateZettelHandler(ItemRepository repository)
{
    public async Task HandleAsync(CreateZettelCommand command)
    {
        var zettel = Item.CreateZettel(
            title: command.Title,
            parentId: new ItemId(command.ParentId)
        );

        await repository.AddAsync(zettel, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
