using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;

namespace Misa.Application.Items.Features.Sessions.Handlers;

public class PauseSessionHandler(IItemRepository repository)
{
    public async Task<Result> Handle(PauseSessionCommand command, CancellationToken ct)
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

        if (item.StateId != (int)Domain.Dictionaries.Items.ItemStates.Active)
        {
            return Result.Invalid("item.not_active", "Item is not active and cannot be paused.");
        }

        var session = await repository.TryGetRunningSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result.NotFound("session.not_found", "Active session not found.");
        }

        session.Pause(command.PauseReason, DateTimeOffset.UtcNow);

        item.Entity.Update();
        item.ChangeState(Domain.Dictionaries.Items.ItemStates.Paused);

        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
