using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Common.Results;
using Misa.Application.Scheduling.Events.Commands;
using Misa.Application.Scheduling.Results;
using Wolverine;

namespace Misa.Application.Scheduling.Commands.Deadlines;

public sealed class RemoveItemDeadlineHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task<Result> Handle(RemoveItemDeadlineCommand command, CancellationToken ct)
    {
        Thread.Sleep(3000);
        if (command.ItemId == Guid.Empty)
        {
            return Result.Invalid(DeadlineErrorCodes.ItemIdEmpty, "ItemId must not be empty.");
        }

        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result.NotFound(DeadlineErrorCodes.ItemNotFound, "Item not found.");
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