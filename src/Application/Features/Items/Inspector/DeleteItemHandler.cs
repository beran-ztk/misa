using Misa.Application.Abstractions.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Inspector;

public record DeleteItemCommand(Guid Id);
public sealed class DeleteItemHandler(IItemRepository repository)
{
    public async Task HandleAsync(DeleteItemCommand command)
    {
        var item = await repository.TryGetItemAsync(command.Id);
        if (item == null)
            throw new DomainNotFoundException("Item not found", "");
        
        item.Delete();
        await repository.SaveChangesAsync(CancellationToken.None);
    }
}