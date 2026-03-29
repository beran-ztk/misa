using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record ContinueSessionCommand(Guid ItemId);
public class ContinueSessionHandler(ItemRepository repository)
{
    public async Task Handle(ContinueSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null || item.Activity.Sessions.Count == 0 || item.Activity.TryGetSession is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.ContinueCurrentSession();

        await repository.SaveChangesAsync(ct);
    }
}
