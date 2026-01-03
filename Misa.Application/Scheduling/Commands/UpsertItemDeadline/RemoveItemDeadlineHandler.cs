using System.ComponentModel.DataAnnotations;
using Misa.Application.Common.Exceptions;
using Misa.Application.Items.Repositories;

namespace Misa.Application.Scheduling.Commands.UpsertItemDeadline;

public sealed class RemoveItemDeadlineHandler(IItemRepository repository)
{
    public async Task Handle(RemoveItemDeadlineCommand command, CancellationToken ct = default)
    {
        if (command.ItemId == Guid.Empty)
            throw new ValidationException("ItemId must not be empty.");

        var item = await repository.GetTrackedItemAsync(command.ItemId);
        if (item is null)
            throw NotFoundException.For("Item", command.ItemId);
        
        await repository.RemoveDeadlineAsync(command.ItemId, ct);
        
        await repository.SaveChangesAsync(ct);
    }
}