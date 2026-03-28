using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Core.Features.Items.Sessions;
public record StopSessionCommand(
    Guid ItemId,
    SessionEfficiencyType SessionEfficiency,
    SessionConcentrationType SessionConcentration,
    string? Summary
);

public class StopSessionHandler(ItemRepository repository)
{
    public async Task Handle(StopSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.StopCurrentSession(
            DateTimeOffset.UtcNow,
            command.SessionEfficiency,
            command.SessionConcentration,
            command.Summary);

        await repository.SaveChangesAsync(ct);
    }
}
