using System.Text.Json;
using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Features.Messaging;
using Misa.Domain.Features.Messaging;

namespace Misa.Application.Features.Entities.Extensions.Items.Features.Scheduling.Commands;

public sealed record SchedulePublishingCommand;
public class SchedulePublishingHandler(ISchedulerRepository repository)
{
    public async Task<List<NotificationDto>> HandleAsync(SchedulePublishingCommand command, CancellationToken stoppingToken)
    {
        List<NotificationDto> messages = [];
        var outboxes = await repository.GetPendingOutboxesAsync(stoppingToken);

        foreach (var entry in outboxes)
        {
            entry.Process();
            messages.Add(new NotificationDto
            {
                NotificationType = NotificationTypeDto.TaskCreated,
                NotificationSeverity = NotificationSeverityDto.Info,
                Payload = entry.Payload,
                Timestamp = entry.CreatedAtUtc
            });
        }
        
        await repository.SaveChangesAsync(stoppingToken);
        
        return messages;
    }
}