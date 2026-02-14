using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Base;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
public class PauseSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result<SessionResolvedDto>> Handle(PauseSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<SessionResolvedDto>.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        if (item.State != ItemState.Active)
        {
            // return Result.Invalid("item.not_active", "Item is not active and cannot be paused.");
        }

        var session = await repository.TryGetRunningSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result<SessionResolvedDto>.NotFound("session.not_found", "Active session not found.");
        }

        session.Pause(command.PauseReason, timeProvider.UtcNow);

        item.Entity.Update(timeProvider.UtcNow);
        item.ChangeState(ItemState.Paused);

        await repository.SaveChangesAsync(ct);

        var createdSession = await repository.TryGetActiveSessionByItemIdAsync(session.Id, ct);
        return createdSession is null 
            ? Result<SessionResolvedDto>.NotFound("session", "session not found.") 
            : Result<SessionResolvedDto>.Ok(createdSession.ToDto());
    }
}
