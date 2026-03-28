using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record RestoreItemCommand(Guid Id);

public sealed class RestoreItemHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task HandleAsync(RestoreItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.Restore(timeProvider.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
