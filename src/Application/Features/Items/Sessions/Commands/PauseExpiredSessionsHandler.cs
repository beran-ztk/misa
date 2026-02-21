using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Application.Features.Items.Sessions.Commands;
public record PauseExpiredSessionsCommand;
public class PauseExpiredSessionsHandler(IItemRepository repository, IMessageBus bus, ITimeProvider  timeProvider)
{
    public async Task Handle(PauseExpiredSessionsCommand command, CancellationToken ct)
    {
        var oldestAllowedTimestamp = timeProvider.UtcNow - TimeSpan.FromHours(18);
        var expiredSessions = await repository.GetInactiveSessionsAsync(oldestAllowedTimestamp, ct);

        foreach (var cmd in expiredSessions.Select(s => new PauseSessionCommand(
                     s.ItemId.Value, 
                     "Session-Segment was automatically paused for running over 18 hours.")))
        {
            await bus.InvokeAsync<Result>(cmd, ct);
        }
    }
}