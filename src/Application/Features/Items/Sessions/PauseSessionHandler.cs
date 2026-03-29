using Misa.Core.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
public class PauseSessionHandler(ItemRepository repository)
{
    public async Task Handle(PauseSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.PauseCurrentSession(command.PauseReason);

        await repository.SaveChangesAsync(ct);
    }
}
