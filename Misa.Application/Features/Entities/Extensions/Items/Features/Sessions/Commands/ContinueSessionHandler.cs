using Misa.Application.Common.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record ContinueSessionCommand(Guid ItemId);
public class ContinueSessionHandler(IItemRepository repository)
{
    public async Task<Result> Handle(ContinueSessionCommand command, CancellationToken ct)
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

        if (item.StateId != (int)ItemStates.Paused)
        {
            return Result.Invalid("item.not_paused", "Item is not paused and cannot be continued.");
        }
        
        var session = await repository.TryGetPausedSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result.NotFound("session.not_found", "Paused session not found.");
        }

        session.Continue(DateTimeOffset.UtcNow);

        item.Entity.Update();
        item.ChangeState(ItemStates.Active);

        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
