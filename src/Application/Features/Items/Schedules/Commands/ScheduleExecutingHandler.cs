using System.Text.Json;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Notifications;

namespace Misa.Application.Features.Items.Schedules.Commands;

public sealed record ScheduleExecutingCommand;

public class ScheduleExecutingHandler(
    ISchedulerExecutingRepository repository,
    INotificationRepository        notificationRepository,
    ITimeProvider                  timeProvider,
    IIdGenerator                   idGenerator)
{
    public async Task HandleAsync(
        ScheduleExecutingCommand command,
        CancellationToken        stoppingToken)
    {
        var pending = await repository.GetPendingWithExtensionsAsync(stoppingToken);

        foreach (var (log, item) in pending)
        {

            log.Claim(timeProvider.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);

            log.Start(timeProvider.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);
            
            
            if (item.ScheduleExtension == null)
            {
                log.Fail(timeProvider.UtcNow, "Has no Schedule extension");
                await repository.SaveChangesAsync(stoppingToken);
                continue;
            }
            
            if (item.ScheduleExtension.ActionType != ScheduleActionType.None)
            {
                log.Fail(timeProvider.UtcNow, "This Action is not implemented yet");
                await repository.SaveChangesAsync(stoppingToken);
                continue;
            }

            var notification = new Notification(
                idGenerator.New(),
                item.Title,
                "Schedule has been executed",
                NotificationSourceKind.SchedulerExecution,
                log.Id,
                timeProvider.UtcNow);

            await notificationRepository.AddAsync(notification, stoppingToken);

            log.Succeeded(timeProvider.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);
            await notificationRepository.SaveChangesAsync(stoppingToken);
        }
    }
}
