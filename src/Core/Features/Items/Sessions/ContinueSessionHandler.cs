using Misa.Core.Abstractions.Ids;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record ContinueSessionCommand(Guid ItemId);
public class ContinueSessionHandler(IItemRepository repository, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task Handle(ContinueSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.ContinueCurrentSession(idGenerator.New(), timeProvider.UtcNow);

        await repository.SaveChangesAsync(ct);
    }
}
