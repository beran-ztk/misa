using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record CreateZettelCommand(string Title, string? Content, Guid TopicId);

public sealed class CreateZettelHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(CreateZettelCommand command)
    {
        var zettel = Item.CreateZettel(
            id: new ItemId(idGenerator.New()),
            ownerId: currentUser.Id,
            title: command.Title,
            content: command.Content,
            createdAtUtc: timeProvider.UtcNow,
            topicId: new ItemId(command.TopicId)
        );

        await repository.AddAsync(zettel, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
