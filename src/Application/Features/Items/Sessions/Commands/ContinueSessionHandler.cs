using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Application.Features.Items.Sessions.Commands;
public record ContinueSessionCommand(Guid ItemId);
public class ContinueSessionHandler(IItemRepository repository, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task Handle(ContinueSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.TryGetSession.Continue(idGenerator.New(), timeProvider.UtcNow);
        
        await repository.SaveChangesAsync(ct);
    }
}
