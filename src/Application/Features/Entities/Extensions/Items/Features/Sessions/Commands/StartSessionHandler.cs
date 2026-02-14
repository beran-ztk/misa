using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Mappings;
using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
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
    public async Task<Result<SessionResolvedDto>> Handle(StartSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<SessionResolvedDto>.NotFound("session.item", "session not found.");
        }

        if (item.State == ItemState.Active)
        {
            // return Result.Invalid("item.already_active", "ItemId is already active!");
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
        item.ChangeState(ItemState.Active);
        
        await repository.AddAsync(session, ct);

        await repository.SaveChangesAsync(ct);
        
        session.AddStartSegment(idGenerator.New());
        await repository.SaveChangesAsync(ct);

        var createdSession = await repository.TryGetActiveSessionByItemIdAsync(session.ItemId, ct);
        return createdSession is null 
            ? Result<SessionResolvedDto>.NotFound("session", "session not found.") 
            : Result<SessionResolvedDto>.Ok(createdSession.ToDto());
    }
}