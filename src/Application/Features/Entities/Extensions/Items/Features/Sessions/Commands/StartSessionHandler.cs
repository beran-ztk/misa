using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Contract.Shared.Results;
using Misa.Domain.Features.Entities.Extensions.Items.Base;
using Misa.Domain.Features.Entities.Extensions.Items.Features.Sessions;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record StartSessionCommand(
    Guid ItemId, 
    TimeSpan? PlannedDuration,
    string? Objective, 
    bool StopAutomatically, 
    string? AutoStopReason
);
public class StartSessionHandler(IItemRepository repository, ITimeProvider timeProvider, IIdGenerator idGenerator)
{
    public async Task<Result> Handle(StartSessionCommand command, CancellationToken ct)
    {
        if (command.ItemId == Guid.Empty)
        {
            return Result.Invalid(ItemErrorCodes.ItemIdEmpty, "ItemId must not be empty.");
        }
        
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result.NotFound(ItemErrorCodes.ItemNotFound, "Item not found.");
        }

        if (item.StateId == (int)ItemStates.Active)
        {
            return Result.Invalid("item.already_active", "ItemId is already active!");
        }

        var session = Session.Start
        (
            idGenerator.New(),
            command.ItemId, 
            command.PlannedDuration, 
            command.Objective, 
            command.StopAutomatically, 
            command.AutoStopReason, 
            timeProvider.UtcNow
        );
        
        item.Entity.Update(timeProvider.UtcNow);
        item.ChangeState(ItemStates.Active);
        
        await repository.AddAsync(session, ct);

        await repository.SaveChangesAsync(ct);
        
        session.AddStartSegment(idGenerator.New());
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}