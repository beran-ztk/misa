using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Application.Items.Features.Sessions.Handlers;

public class PauseExpiredSessionsHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task Handle(PauseExpiredSessionsCommand command, CancellationToken ct)
    {
        var oldestAllowedTimestamp = DateTimeOffset.UtcNow - TimeSpan.FromHours(18);
        var expiredSessions = await repository.GetInactiveSessionsAsync(oldestAllowedTimestamp, ct);

        foreach (var cmd in expiredSessions.Select(s => new PauseSessionCommand(
                     s.ItemId, 
                     "Session-Segment was automatically paused for running over 18 hours.")))
        {
            await bus.InvokeAsync<Result>(cmd, ct);
        }
    }
}