using Misa.Core.Abstractions.Ids;
using Misa.Core.Abstractions.Persistence;
using Misa.Core.Abstractions.Time;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record StartSessionCommand(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);
public class StartSessionHandler(IItemRepository repository, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task Handle(StartSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.StartSession(
            sessionId: idGenerator.New(),
            segmentId: idGenerator.New(),
            command.PlannedDuration, 
            command.Objective, 
            command.StopAutomatically, 
            command.AutoStopReason, 
            timeProvider.UtcNow
        );
        
        await repository.SaveChangesAsync(ct);
    }
}