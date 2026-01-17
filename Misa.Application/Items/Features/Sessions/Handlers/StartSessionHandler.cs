using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Misa.Domain.Audit;

namespace Misa.Application.Items.Features.Sessions.Handlers;

public class StartSessionHandler(IItemRepository repository)
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

        if (item.StateId == (int)Domain.Dictionaries.Items.ItemStates.Active)
        {
            return Result.Invalid("item.already_active", "ItemId is already active!");
        }

        var session = Session.Start
        (
            command.ItemId, 
            command.PlannedDuration, 
            command.Objective, 
            command.StopAutomatically, 
            command.AutoStopReason, 
            DateTimeOffset.UtcNow
        );
        
        item.Entity.Update();
        item.ChangeState(Domain.Dictionaries.Items.ItemStates.Active);
        
        await repository.AddAsync(session, ct);

        await repository.SaveChangesAsync(ct);
        
        session.AddStartSegment();
        await repository.SaveChangesAsync(ct);

        return Result.Ok();
    }
}