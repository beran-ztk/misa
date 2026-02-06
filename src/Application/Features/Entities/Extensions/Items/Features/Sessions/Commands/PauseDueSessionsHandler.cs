using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Sessions.Commands;
public record PauseDueSessionsCommand;
public class PauseDueSessionsHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task<int> Handle(PauseDueSessionsCommand command, CancellationToken ct)
    {
        var stopped = 0;
        
        var dueSessions = await repository.GetActiveSessionsWithAutostopAsync(ct);

        foreach (var s in dueSessions.Where(s => s.ElapsedTime >= s.PlannedDuration))
        {
            s.Autostop();
            
            var cmd = new PauseSessionCommand(
                s.ItemId,
                "Autostop has been triggered. Elapsed time exceeds planned Duration");
            
            await bus.InvokeAsync<Result>(cmd, ct);

            stopped++;
        }

        if (stopped > 0)
            await repository.SaveChangesAsync(ct);
        
        return stopped;
    }
}