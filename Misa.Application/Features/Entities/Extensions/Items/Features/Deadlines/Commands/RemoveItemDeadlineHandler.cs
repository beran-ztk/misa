using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Events;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Deadlines.Commands;

public sealed class RemoveItemDeadlineHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task<Result> Handle(RemoveItemDeadlineCommand command, CancellationToken ct)
    {
        if (command.ItemId == Guid.Empty)
        {
            return Result.Invalid(ItemErrorCodes.ItemIdEmpty, "ItemId must not be empty.");
        }

        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        var deadlineEntry = await repository.TryGetScheduledDeadlineForItemAsync(command.ItemId, ct);
        if (deadlineEntry is null)
        {
            return Result.NotFound(DeadlineErrorCodes.DeadlineNotFound, "Deadline not found.");
        }

        deadlineEntry.RemoveDeadlineAndAuditChanges();
        await repository.RemoveScheduledDeadlineAsync(deadlineEntry, ct);
        
        await repository.SaveChangesAsync(ct);

        await bus.PublishAsync(new ItemDeadlineRemovedEvent(command.ItemId));

        return Result.Ok();
    }
}