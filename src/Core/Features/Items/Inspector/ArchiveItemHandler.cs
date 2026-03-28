using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record ArchiveItemCommand(Guid Id);
public sealed class ArchiveItemHandler(ItemRepository repository)
{
    public async Task HandleAsync(ArchiveItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.Archive();
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
