using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Sessions.Commands;
public record PauseSessionCommand(
    Guid ItemId,
    string? PauseReason
);
public class PauseSessionHandler(IItemRepository repository, ITimeProvider timeProvider)
{
    public async Task Handle(PauseSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.TryGetSession.Pause(command.PauseReason, timeProvider.UtcNow);

        await repository.SaveChangesAsync(ct);
    }
}
