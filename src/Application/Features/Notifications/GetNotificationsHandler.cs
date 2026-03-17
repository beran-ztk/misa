using Misa.Application.Abstractions.Persistence;
using Misa.Contract.Notifications;
using Misa.Domain.Notifications;

namespace Misa.Application.Features.Notifications;

public sealed record GetNotificationsQuery(int Limit = 25, DateTimeOffset? Before = null, bool OnlyUnread = false);

public class GetNotificationsHandler(INotificationRepository repository)
{
    public async Task<List<NotificationEntryDto>> HandleAsync(
        GetNotificationsQuery query,
        CancellationToken ct)
    {
        var notifications = await repository.GetPageAsync(query.Limit, query.Before, query.OnlyUnread, ct);

        return notifications
            .Select(n => new NotificationEntryDto(
                n.Id,
                n.Title,
                n.Message,
                n.SourceId,
                n.CreatedAtUtc,
                n.ReadAtUtc,
                ResolveLinkTarget(n.SourceKind, n.SourceId)))
            .ToList();
    }

    private static NotificationLinkTarget? ResolveLinkTarget(NotificationSourceKind kind, Guid sourceId) =>
        kind switch
        {
            NotificationSourceKind.SessionPlannedDurationReached =>
                new NotificationLinkTarget(NotificationWorkspaceTarget.Tasks, sourceId),
            _ => null
        };
}
