using Misa.Application.Abstractions.Ids;
using Misa.Application.Abstractions.Notifications;
using Misa.Application.Abstractions.Persistence;
using Misa.Application.Abstractions.Time;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Notifications;

namespace Misa.Application.Features.Items.Sessions.Commands;

public record NotifySessionPlannedDurationReachedCommand;

public class NotifySessionPlannedDurationReachedHandler(
    IItemRepository          itemRepository,
    INotificationRepository  notificationRepository,
    ITimeProvider            timeProvider,
    IIdGenerator             idGenerator,
    INotificationPushService pushService)
{
    public async Task Handle(NotifySessionPlannedDurationReachedCommand command, CancellationToken ct)
    {
        var sessions = await itemRepository.GetSessionsForDurationNotificationAsync(ct);
        var now = timeProvider.UtcNow;

        foreach (var session in sessions)
        {
            var elapsed = CalculateElapsed(session, now);
            if (elapsed < session.PlannedDuration!.Value)
                continue;

            session.MarkPlannedDurationNotificationSent();

            var notification = new Notification(
                idGenerator.New(),
                "Planned duration reached",
                $"The planned duration for \"{session.Item.Title}\" has been reached.",
                NotificationSourceKind.SessionPlannedDurationReached,
                session.Item.Id.Value,
                now,
                session.Item.OwnerId);

            await notificationRepository.AddAsync(notification, ct);
            await itemRepository.SaveChangesAsync(ct);
            await notificationRepository.SaveChangesAsync(ct);
            await pushService.NotifyUserChangedAsync(session.Item.OwnerId);
        }
    }

    private static TimeSpan CalculateElapsed(Session session, DateTimeOffset now) =>
        session.Segments.Aggregate(
            TimeSpan.Zero,
            (acc, seg) => acc + ((seg.EndedAtUtc ?? now) - seg.StartedAtUtc));
}
