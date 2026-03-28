using Misa.Core.Common.Abstractions.Persistence;
using Misa.Core.Common.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record ArchiveItemCommand(Guid Id);
public sealed class ArchiveItemHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(ArchiveItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.Archive(timeProvider.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
