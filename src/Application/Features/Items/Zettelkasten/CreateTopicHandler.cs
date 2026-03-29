using Misa.Core.Persistence;
using Misa.Domain.Items;

namespace Misa.Core.Features.Items.Zettelkasten;

public sealed record CreateTopicCommand(string Title, Guid? ParentId);

public sealed class CreateTopicHandler(ItemRepository repository)
{
    public async Task HandleAsync(CreateTopicCommand command)
    {
        ItemId? parentId = command.ParentId is null
            ? null
            : new ItemId(command.ParentId.Value);

        var topic = Item.CreateTopic(
            title: command.Title,
            parentId: parentId
        );

        await repository.AddAsync(topic, CancellationToken.None);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
