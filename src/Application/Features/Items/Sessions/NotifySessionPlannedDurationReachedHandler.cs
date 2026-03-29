using Misa.Core.Common.Abstractions.Persistence;
using Misa.Domain.Items.Components.Activities.Sessions;
using Misa.Domain.Notifications;

namespace Misa.Core.Features.Items.Sessions;

public record NotifySessionPlannedDurationReachedCommand;

public class NotifySessionPlannedDurationReachedHandler(
    ItemRepository          itemRepository,
    NotificationRepository  notificationRepository)
{
    public async Task Handle(NotifySessionPlannedDurationReachedCommand command, CancellationToken ct)
    {
        var sessions = await itemRepository.GetSessionsForDurationNotificationAsync(ct);
        var now = DateTimeOffset.UtcNow;

        foreach (var session in sessions)
        {
            var elapsed = CalculateElapsed(session, now);
            if (elapsed < session.PlannedDuration!.Value)
                continue;

            session.MarkPlannedDurationNotificationSent();

            var notification = new Notification(
                "Planned duration reached",
                $"The planned duration for \"{session.Item.Title}\" has been reached.",
                NotificationSourceKind.SessionPlannedDurationReached,
                session.Item.Id.Value);

            await notificationRepository.AddAsync(notification, ct);
            await itemRepository.SaveChangesAsync(ct);
            await notificationRepository.SaveChangesAsync(ct);
        }
    }

    private static TimeSpan CalculateElapsed(Session session, DateTimeOffset now) =>
        session.Segments.Aggregate(
            TimeSpan.Zero,
            (acc, seg) => acc + ((seg.EndedAtUtc ?? now) - seg.StartedAtUtc));
}
