using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Inspector;

public record RenameItemCommand(Guid Id, string Title);

public sealed class RenameItemHandler(ItemRepository repository)
{
    public async Task HandleAsync(RenameItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");

        item.ChangeTitle(command.Title, DateTimeOffset.UtcNow);
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}
