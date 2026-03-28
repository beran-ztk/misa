using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Core.Mappings;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record StopSessionCommand(
    Guid ItemId,
    SessionEfficiencyDto SessionEfficiency,
    SessionConcentrationDto SessionConcentration,
    string? Summary
);

public class StopSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task Handle(StopSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.StopCurrentSession(
            timeProvider.UtcNow,
            command.SessionEfficiency.ToDomain(),
            command.SessionConcentration.ToDomain(),
            command.Summary);

        await repository.SaveChangesAsync(ct);
    }
}
