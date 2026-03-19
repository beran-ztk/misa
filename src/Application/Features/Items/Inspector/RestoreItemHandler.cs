using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Inspector;

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
