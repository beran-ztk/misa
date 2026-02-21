using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Common.Results;
using Misa.Domain.Items.Components.Activities;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record ContinueSessionCommand(Guid ItemId);
public class ContinueSessionHandler(IItemRepository repository, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task<Result> Handle(ContinueSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }
        
        var session = await repository.TryGetPausedSessionByItemIdAsync(command.ItemId, ct);
        if (session is null)
        {
            return Result.NotFound("session.not_found", "Paused session not found.");
        }

        session.Continue(idGenerator.New(), timeProvider.UtcNow);
        
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
