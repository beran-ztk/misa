using System.Text.Json;
using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Notifications;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Application.Mappings;
using Misa.Contract.Items.Components.Schedules;
using Misa.Domain.Items;
using Misa.Domain.Items.Components.Schedules;
using Misa.Domain.Notifications;

namespace Misa.Application.Features.Items.Schedules.Commands;

public sealed record ScheduleExecutingCommand;

public class ScheduleExecutingHandler(
    ISchedulerExecutingRepository repository,
    IItemRepository               itemRepository,
    INotificationRepository       notificationRepository,
    ITimeProvider                 timeProvider,
    IIdGenerator                  idGenerator,
    INotificationPushService      pushService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

            string notificationTitle;
            string notificationMessage;
            var    notificationSourceKind = NotificationSourceKind.SchedulerExecution;
            var    notificationSourceId   = log.Id;

            switch (item.ScheduleExtension.ActionType)
            {
                case ScheduleActionType.None:
                    notificationTitle   = item.Title;
                    notificationMessage = "Schedule has been executed";
                    break;

                case ScheduleActionType.CreateTask:
                    var taskResult = await ExecuteCreateTaskAsync(
                        item.ScheduleExtension, item.OwnerId, stoppingToken);

                    if (taskResult is null)
                    {
                        log.Fail(timeProvider.UtcNow, "CreateTask payload is missing or invalid");
                        await repository.SaveChangesAsync(stoppingToken);
                        continue;
                    }

                    notificationTitle        = item.Title;
                    notificationMessage      = $"Task \"{taskResult.Value.Title}\" was created";
                    notificationSourceKind   = NotificationSourceKind.ScheduleCreatedTask;
                    notificationSourceId     = taskResult.Value.Id;
                    break;

                default:
                    log.Fail(timeProvider.UtcNow, $"Action type '{item.ScheduleExtension.ActionType}' is not implemented");
                    await repository.SaveChangesAsync(stoppingToken);
                    continue;
            }

            var notification = new Notification(
                idGenerator.New(),
                notificationTitle,
                notificationMessage,
                notificationSourceKind,
                notificationSourceId,
                timeProvider.UtcNow,
                item.OwnerId);

            await notificationRepository.AddAsync(notification, stoppingToken);

            log.Succeeded(timeProvider.UtcNow);
            await repository.SaveChangesAsync(stoppingToken);
            await notificationRepository.SaveChangesAsync(stoppingToken);
            await pushService.NotifyUserChangedAsync(item.OwnerId);
        }
    }

    /// <summary>
    /// Deserializes the CreateTask payload, creates the task, and returns its ID + title.
    /// Returns null if the payload is missing or invalid.
    /// </summary>
    private async Task<(Guid Id, string Title)?> ExecuteCreateTaskAsync(
        ScheduleExtension schedule,
        string            ownerId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(schedule.Payload))
            return null;

        CreateTaskSchedulePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<CreateTaskSchedulePayload>(schedule.Payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Title))
            return null;

        var taskId = idGenerator.New();
        var task   = Item.CreateTask(
            id:           new ItemId(taskId),
            ownerId:      ownerId,
            title:        payload.Title,
            description:  payload.Description,
            category:     payload.Category.ToDomain(),
            createdAtUtc: timeProvider.UtcNow,
            priority:     payload.Priority.ToDomain(),
            dueAt:        null);

        await itemRepository.AddAsync(task, ct);
        await itemRepository.SaveChangesAsync(ct);

        return (taskId, payload.Title);
    }
}
