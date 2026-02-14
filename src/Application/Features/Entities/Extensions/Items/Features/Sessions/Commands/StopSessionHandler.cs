using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Application.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record StopSessionCommand(
    Guid ItemId,
    EfficiencyContract Efficiency,
    ConcentrationContract Concentration,
    string? Summary
);

public class StopSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result<SessionResolvedDto>> Handle(StopSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<SessionResolvedDto>.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        if (item.State is not ItemState.Active 
            && item.State is not ItemState.Paused)
        {
            // return Result.Invalid("item.no_session_state", "Item is not active or paused and cannot be stopped.");
        }

        var session = await repository.TryGetActiveSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result<SessionResolvedDto>.NotFound("session.not_found", "Active session not found.");
        }

        session.Stop(
            timeProvider.UtcNow,
            command.Efficiency.MapToDomain(),
            command.Concentration.MapToDomain(),
            command.Summary
        );

        item.Entity.Update(timeProvider.UtcNow);
        item.ChangeState(ItemState.InProgress);

        await repository.SaveChangesAsync(ct);

        var createdSession = await repository.TryGetActiveSessionByItemIdAsync(session.Id, ct);
        return createdSession is null 
            ? Result<SessionResolvedDto>.NotFound("session", "session not found.") 
            : Result<SessionResolvedDto>.Ok(createdSession.ToDto());
    }
}