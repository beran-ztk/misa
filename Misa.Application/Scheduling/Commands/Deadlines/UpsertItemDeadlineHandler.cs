using System.ComponentModel.DataAnnotations;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Exceptions;

namespace Misa.Application.Scheduling.Commands.Deadlines;

public sealed class UpsertItemDeadlineHandler(IItemRepository repository)
{
    public async Task Handle(UpsertItemDeadlineCommand command, CancellationToken ct = default)
    {
        if (command.ItemId == Guid.Empty)
            throw new ValidationException("ItemId must not be empty.");

        var item = await repository.GetTrackedItemAsync(command.ItemId);
        if (item is null)
            throw NotFoundException.For("Item", command.ItemId);

        var dueAtUtc = command.DueAt.ToUniversalTime();
        await repository.UpsertDeadlineAsync(command.ItemId, dueAtUtc, ct);
        
        await repository.SaveChangesAsync(ct);
    }
}