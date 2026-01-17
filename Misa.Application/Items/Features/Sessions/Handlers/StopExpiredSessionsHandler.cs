using Misa.Application.Common.Abstractions.Persistence;
using Misa.Application.Items.Features.Sessions.Commands;
using Misa.Contract.Common.Results;
using Wolverine;

namespace Misa.Application.Items.Features.Sessions.Handlers;

public class StopExpiredSessionsHandler(IItemRepository repository, IMessageBus bus)
{
    public async Task<int> Handle(StopExpiredSessionsCommand command, CancellationToken ct)
    {
        Console.WriteLine("Start StopExpiredSessionsHandler");
        
        var stopped = 0;
        
        var dueSessions = await repository.GetActiveSessionsWithAutostopAsync(ct);

        foreach (var s in dueSessions.Where(s => s.ElapsedTime >= s.PlannedDuration))
        {
            Console.WriteLine($"Autostop {s.ItemId}.");
            s.Autostop();
            
            var stopCommand = new StopSessionCommand(
                s.ItemId, 
                null, 
                null, 
                "Automatically stopped.");
            
            await bus.InvokeAsync<Result>(stopCommand, ct);

            stopped++;
        }

        if (stopped > 0)
            await repository.SaveChangesAsync(ct);
        
        return stopped;
    }
}