using System.Text.Json;
using Misa.Application.Common.Abstractions.Persistence;
using Misa.Domain.Features.Messaging;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record SchedulePublishingCommand;
public class SchedulePublishingHandler(ISchedulerRepository repository)
{
    public async Task<List<string>> HandleAsync(SchedulePublishingCommand command, CancellationToken stoppingToken)
    {
        List<string> messages = [];
        var outboxes = await repository.GetPendingOutboxesAsync(stoppingToken);

        foreach (var entry in outboxes)
        {
            entry.Process();
            messages.Add($"Event: {entry.EventType.ToString()} - Note: {entry.Payload} at {entry.CreatedAtUtc}");
        }
        
        await repository.SaveChangesAsync(stoppingToken);
        
        return messages;
    }
}