using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Exceptions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Features.Items.Sessions.Commands;
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