using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Shared.Results;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
public class PauseSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result<SessionDto>> Handle(PauseSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<SessionDto>.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        var session = await repository.TryGetRunningSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result<SessionDto>.NotFound("session.not_found", "Active session not found.");
        }

        session.Pause(command.PauseReason, timeProvider.UtcNow);

        await repository.SaveChangesAsync(ct);

        var createdSession = await repository.TryGetActiveSessionByItemIdAsync(session.ItemId.Value, ct);
        return createdSession is null 
            ? Result<SessionDto>.NotFound("session", "session not found.") 
            : Result<SessionDto>.Ok(createdSession.ToDto());
    }
}
