using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Common.Results;
using Misa.Contract.Items.Components.Activity.Sessions;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Activities.Sessions;

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
    public async Task<Result<SessionDto>> Handle(StartSessionCommand command, CancellationToken ct)
    {
        var item = await repository.TryGetItemAsync(command.ItemId, ct);
        if (item is null)
        {
            return Result<SessionDto>.NotFound("session.item", "session not found.");
        }

        var session = Session.Start
        (
            idGenerator.New(),
            new ItemId(command.ItemId), 
            command.PlannedDuration, 
            command.Objective, 
            command.StopAutomatically, 
            command.AutoStopReason, 
            timeProvider.UtcNow
        );
        await repository.AddAsync(session, ct);

        await repository.SaveChangesAsync(ct);
        
        session.AddStartSegment(idGenerator.New());
        await repository.SaveChangesAsync(ct);

        var createdSession = await repository.TryGetActiveSessionByItemIdAsync(session.ItemId.Value, ct);
        return createdSession is null 
            ? Result<SessionDto>.NotFound("session", "session not found.") 
            : Result<SessionDto>.Ok(createdSession.ToDto());
    }
}