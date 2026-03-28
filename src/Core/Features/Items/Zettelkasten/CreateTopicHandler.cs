using Misa.Core.Abstractions.Ids;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record CreateTopicCommand(string Title, Guid? ParentId);

public sealed class CreateTopicHandler(
    IItemRepository repository,
    ITimeProvider timeProvider,
    IIdGenerator idGenerator)
{
    public async Task HandleAsync(CreateTopicCommand command)
    {
        ItemId? parentId = command.ParentId is null
            ? null
            : new ItemId(command.ParentId.Value);

        var topic = Item.CreateTopic(
            id: new ItemId(idGenerator.New()),
            title: command.Title,
            createdAtUtc: timeProvider.UtcNow,
            parentId: parentId
        );

        await repository.AddAsync(topic, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
