using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Contract.Shared.Results;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record StopSessionCommand(
    Guid ItemId,
    SessionEfficiencyDto SessionEfficiency,
    SessionConcentrationDto SessionConcentration,
    string? Summary
);

public class StopSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task<Result> Handle(StopSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        var session = await repository.TryGetActiveSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result.NotFound("session.not_found", "Active session not found.");
        }

        session.Stop(
            timeProvider.UtcNow,
            command.SessionEfficiency.ToDomain(),
            command.SessionConcentration.ToDomain(),
            command.Summary
        );

        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}