using Misa.Core.Common.Abstractions.Ids;
using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record CreateZettelCommand(string Title, Guid ParentId);

public sealed class CreateZettelHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator)
{
    public async Task HandleAsync(CreateZettelCommand command)
    {
        var zettel = Item.CreateZettel(
            id: new ItemId(idGenerator.New()),
            title: command.Title,
            createdAtUtc: timeProvider.UtcNow,
            parentId: new ItemId(command.ParentId)
        );

        await repository.AddAsync(zettel, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
