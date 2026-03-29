using Misa.Core.Persistence;
using Misa.Domain.Exceptions;

namespace Misa.Core.Features.Items.Sessions;
public record StartSessionCommand(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);
public class StartSessionHandler(ItemRepository repository)
{
    public async Task Handle(StartSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemWithSessionsAsync(command.ItemId, ct);
        if (item?.Activity is null)
            throw new DomainNotFoundException("session.item", "session not found.");

        item.Activity.StartSession(
            command.PlannedDuration,
            command.Objective,
            command.StopAutomatically,
            command.AutoStopReason
        );
        
        await repository.SaveChangesAsync(ct);
    }
}