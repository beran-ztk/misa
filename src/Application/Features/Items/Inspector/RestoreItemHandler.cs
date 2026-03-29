using Misa.Core.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record RestoreItemCommand(Guid Id);

public sealed class RestoreItemHandler(ItemRepository repository)
{
    public async Task HandleAsync(RestoreItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.Restore();
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
