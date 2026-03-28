using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record DeleteItemCommand(Guid Id);
public sealed class DeleteItemHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(DeleteItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.Delete(timeProvider.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
