using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
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
    public async Task<Result> Handle(StopSessionCommand command, CancellationToken ct)
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

        if (item.StateId is not (int)ItemStates.Active 
            && item.StateId is not (int)ItemStates.Paused)
        {
            return Result.Invalid("item.no_session_state", "Item is not active or paused and cannot be stopped.");
        }

        var session = await repository.TryGetActiveSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result.NotFound("session.not_found", "Active session not found.");
        }

        session.Stop(
            timeProvider.UtcNow,
            command.Efficiency.MapToDomain(),
            command.Concentration.MapToDomain(),
            command.Summary
        );

        item.Entity.Update(timeProvider.UtcNow);
        item.ChangeState(ItemStates.InProgress);

        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}