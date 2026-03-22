using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;

namespace Misa.Application.Features.Items.Zettelkasten;

public record ReparentKnowledgeItemCommand(Guid ItemId, Guid NewParentId);

public sealed class ReparentKnowledgeItemHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(ReparentKnowledgeItemCommand command)
    {
        if (command.ItemId == command.NewParentId)
            throw new DomainValidationException("newParentId", "invalid_parent", "An item cannot be its own parent.");

        var item = await repository.TryGetKnowledgeIndexItemAsync(command.ItemId, CancellationToken.None);
        if (item is null)
            throw new DomainNotFoundException("Item not found", "");

        item.ReparentKnowledgeIndex(new ItemId(command.NewParentId), timeProvider.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
