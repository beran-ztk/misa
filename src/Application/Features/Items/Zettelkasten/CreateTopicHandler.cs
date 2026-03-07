using Misa.Application.Abstractions.Authentication;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Zettelkasten;

public sealed record CreateTopicCommand(string Title, Guid? ParentId);

public sealed class CreateTopicHandler(
    IItemRepository repository, 
    ITimeProvider timeProvider, 
    IIdGenerator idGenerator,
    ICurrentUser currentUser)
{
    public async Task HandleAsync(CreateTopicCommand command)
    {
        ItemId? parentId = command.ParentId is null
            ? null
            : new ItemId(command.ParentId.Value);
        
        var topic = Item.CreateTopic(
            id: new ItemId(idGenerator.New()), 
            ownerId: currentUser.Id,
            title: command.Title,
            createdAtUtc: timeProvider.UtcNow,
            topicId: parentId
        );

        await repository.AddAsync(topic, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}